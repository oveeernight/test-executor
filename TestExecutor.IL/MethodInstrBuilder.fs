module TestExecutor.IL.MethodInstrBuilder

open System.Collections.Generic
open System.Reflection.Emit
open TestExecutor

let private instrEq instr1 instr2 =
    Microsoft.FSharp.Core.LanguagePrimitives.PhysicalEquality instr1 instr2


let private invalidProgram reason =
    raise <| OpCodes.IncorrectCIL reason

let build (bytecode : byte array) =
    let instList = ResizeArray<ilInstr>()
    let codeSize = bytecode.Length
    let mutable offset = 0
    let codeSize : offset = codeSize
    while offset < codeSize do
        let startOffset = offset
        let op = OpCodes.getOpCode bytecode offset
        offset <- offset + op.Size

        let size =
            match op.OperandType with
            | OperandType.InlineNone
            | OperandType.InlineSwitch -> 0
            | OperandType.ShortInlineVar
            | OperandType.ShortInlineI
            | OperandType.ShortInlineBrTarget -> 1
            | OperandType.InlineVar -> 2
            | OperandType.InlineI
            | OperandType.InlineMethod
            | OperandType.InlineType
            | OperandType.InlineString
            | OperandType.InlineSig
            | OperandType.InlineTok
            | OperandType.ShortInlineR
            | OperandType.InlineField
            | OperandType.InlineBrTarget -> 4
            | OperandType.InlineI8
            | OperandType.InlineR -> 8
            | _ -> __unreachable__()

        if offset + size > codeSize then invalidProgram "IL stream unexpectedly ended!"

        let instr = {opcode = op; offset = startOffset}
        instList.Add(instr)
        offset <- offset + size
    let dictionary = Dictionary()
    for inst in instList do
        dictionary[inst.offset] = inst |> ignore
    assert(offset = codeSize)
    instList, dictionary