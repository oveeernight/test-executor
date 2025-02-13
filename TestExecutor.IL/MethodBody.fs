namespace TestExecutor.IL

open System
open System.Reflection
open System.Reflection.Emit
open TestExecutor.IL
open TestExecutor.Utils
open TestExecutor

type ehcType =
    | Filter of offset
    | Catch of Type
    | Finally
    | Fault
    
type ehClause = {
    tryOffset: offset
    tryLength: offset
    handlerOffset: offset
    handlerLength: offset
    ehcType : ehcType
} with
    static member Create (eh : ExceptionHandlingClause) =
        let flags = eh.Flags
        let ehcType =
            if flags = ExceptionHandlingClauseOptions.Filter then Filter eh.FilterOffset
            elif flags = ExceptionHandlingClauseOptions.Finally then Finally
            elif flags = ExceptionHandlingClauseOptions.Fault then Fault
            else Catch eh.CatchType
        {
            tryOffset = eh.TryOffset
            tryLength = eh.TryLength
            handlerOffset = eh.HandlerOffset
            handlerLength = eh.HandlerLength
            ehcType = ehcType
        }
    
module MethodWithBody = 
    let resolveMethodFromMetadata method ilBytes offset =
        let method =
            NumberCreator.extractInt32 ilBytes offset
            |> Reflection.resolveMethod method
        method

    let parseCallSite method ilBytes pos =
        let opCode = OpCodes.getOpCode ilBytes pos
        let calledMethod = resolveMethodFromMetadata method ilBytes (pos + opCode.Size)
        opCode, calledMethod

type ipTransition =
    | FallThrough of offset
    | Return
    | UnconditionalBranch of offset
    | ConditionalBranch of offset * offset list
    // TODO: use this thing? #do
    | ExceptionMechanism

module MethodBody =
    let private operandType2operandSize =
        [|
            4; 4; 4; 8; 4
            0; -1; 8; 4; 4
            4; 4; 4; 4; 2
            1; 1; 4; 1
        |]
    let private jumpTargetsForNext (opCode : OpCode) _ (pos : offset) =
            let nextInstruction = pos +  opCode.Size + operandType2operandSize[int opCode.OperandType]
            FallThrough nextInstruction

    let private jumpTargetsForBranch (opCode : OpCode) ilBytes (pos : offset) =
        let opcodeSize =  opCode.Size
        let offset =
            match opCode.OperandType with
            | OperandType.InlineBrTarget -> NumberCreator.extractInt32 ilBytes (pos + opcodeSize)
            | _ -> NumberCreator.extractInt8 ilBytes (pos + opcodeSize)

        let nextInstruction = pos +  opCode.Size + operandType2operandSize[int opCode.OperandType]
        if offset = 0 && opCode <> OpCodes.Leave && opCode <> OpCodes.Leave_S
        then UnconditionalBranch nextInstruction
        else UnconditionalBranch <|  offset + nextInstruction

    let private inlineBrTarget extract (opCode : OpCode) ilBytes (pos : offset) =
        let opcodeSize =  opCode.Size
        let offset = extract ilBytes (pos + opcodeSize)
        let nextInstruction = pos + opcodeSize + operandType2operandSize[int opCode.OperandType]
        ConditionalBranch(nextInstruction, [nextInstruction + offset])

    let private inlineSwitch (opCode : OpCode) ilBytes (pos : offset) =
        let opcodeSize =  opCode.Size
        let n = NumberCreator.extractUnsignedInt32 ilBytes (pos + opcodeSize) |> int
        let nextInstruction = pos + opcodeSize + 4 * n + 4
        let nextOffsets =
            List.init n (fun x -> nextInstruction +  (NumberCreator.extractInt32 ilBytes (pos + opcodeSize + 4 * (x + 1))))
        ConditionalBranch(nextInstruction, nextOffsets)

    let private jumpTargetsForReturn _ _ _ = Return
    let private jumpTargetsForThrow _ _ _ = ExceptionMechanism

    let findNextInstructionOffsetAndEdges (opCode : OpCode) =
        match opCode.FlowControl with
        | FlowControl.Next
        | FlowControl.Call
        | FlowControl.Break
        | FlowControl.Meta -> jumpTargetsForNext
        | FlowControl.Branch -> jumpTargetsForBranch
        | FlowControl.Cond_Branch ->
            match opCode.OperandType with
            | OperandType.InlineBrTarget -> inlineBrTarget NumberCreator.extractOffset
            | OperandType.ShortInlineBrTarget -> inlineBrTarget NumberCreator.extractInt8
            | OperandType.InlineSwitch -> inlineSwitch
            | _ -> __notImplemented__()
        | FlowControl.Return -> jumpTargetsForReturn
        | FlowControl.Throw -> jumpTargetsForThrow
        | _ -> __notImplemented__()
        <| opCode
        
    let private isCallOpCode (opCode : OpCode) =
            opCode = OpCodes.Call
            //|| opCode = OpCodes.Calli
            || opCode = OpCodes.Callvirt
            || opCode = OpCodes.Tailcall
    let private isNewObjOpCode (opCode : OpCode) =
        opCode = OpCodes.Newobj
    let isDemandingCallOpCode (opCode : OpCode) =
        isCallOpCode opCode || isNewObjOpCode opCode
