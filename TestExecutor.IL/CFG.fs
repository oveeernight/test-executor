namespace Cfg

open System.Collections.Concurrent
open TestExecutor.IL
open TestExecutor.Utils.Collections
open TestExecutor.Utils.Reflection
open global.System
open System.Reflection
open System.Collections.Generic
open Microsoft.FSharp.Collections
open TestExecutor.Utils.GraphUtils

type [<Measure>] terminalSymbol

type ICfgNode =
    inherit IGraphNode<ICfgNode>
    abstract Offset : int


type BasicBlock (method: Method, startOffset: int) =
    let mutable finalOffset = startOffset
    let mutable startOffset = startOffset
    let mutable isCovered = false
    let incomingCFGEdges = HashSet<BasicBlock>()
    let outgoingEdges = Dictionary<int<terminalSymbol>, HashSet<BasicBlock>>()

    member this.StartOffset
        with get () = startOffset
        and internal set v = startOffset <- v
    member this.Method = method
    member this.OutgoingEdges = outgoingEdges
    member this.IncomingCFGEdges = incomingCFGEdges
    member this.IsCovered
        with get () = isCovered
        and set v = isCovered <- v
    member this.HasSiblings
        with get () =
            let siblings = HashSet<BasicBlock>()
            for bb in incomingCFGEdges do
                for kvp in bb.OutgoingEdges do
                        siblings.UnionWith kvp.Value
            siblings.Count > 1

    member this.FinalOffset
        with get () = finalOffset
        and internal set (v : int) = finalOffset <- v

    member private this.GetInstructions() =
        let parsedInstructions = method.Instructions
        let mutable currInstrIdx : int = parsedInstructions.FindIndex(fun instr -> instr.offset = startOffset)
        let mutable notEnd = true
        seq {
            while notEnd do
                let currInstr = parsedInstructions[currInstrIdx]
                notEnd <- this.FinalOffset <> currInstr.offset 
                yield currInstr
                currInstrIdx <- currInstrIdx + 1
        }

    member this.BlockSize with get() =
        this.GetInstructions() |> Seq.length

    interface ICfgNode with
        member this.OutgoingEdges
            with get () =
                let exists, cfgEdges = outgoingEdges.TryGetValue CfgInfo.TerminalForCFGEdge
                if exists
                then cfgEdges |> Seq.cast<ICfgNode>
                else Seq.empty
        member this.Offset = startOffset


and CfgInfo internal (method : Method) =
    let ilBytes = method.Bytecode
    let exceptionHandlers = method.EhClauses
    let sortedBasicBlocks = ResizeArray<BasicBlock>()
    let mutable methodSize = 0
    let sinks = ResizeArray<_>()
    let loopEntries = HashSet<offset>()

    let dfs (startVertices : array<offset>) =
        let used = HashSet<offset>()
        let basicBlocks = HashSet<BasicBlock>()
        let addBasicBlock v = basicBlocks.Add v |> ignore
        let greyVertices = HashSet<offset>()
        let vertexToBasicBlock: array<Option<BasicBlock>> = Array.init ilBytes.Length (fun _ -> None)

        let findFinalVertex intermediatePoint block =
            let mutable index = 0
            let mutable currentIndex = int intermediatePoint - 1
            let mutable found = false
            while not found do
                match vertexToBasicBlock[currentIndex] with
                | Some basicBlock when basicBlock = block ->
                    found <- true
                    index <- currentIndex
                | _ -> currentIndex <- currentIndex - 1

            found <- false
            let offsetToInstr = method.OffsetToInstr
            while not found do
                if offsetToInstr.ContainsKey index then
                    found <- true
                else index <- index - 1
            index

        let splitBasicBlock (block : BasicBlock) intermediatePoint =

            let newBlock = BasicBlock(method, block.StartOffset)
            addBasicBlock newBlock
            block.StartOffset <- intermediatePoint

            newBlock.FinalOffset <- findFinalVertex intermediatePoint block
            for v in int newBlock.StartOffset .. int intermediatePoint - 1 do
                vertexToBasicBlock[v] <- Some newBlock

            for parent in block.IncomingCFGEdges do
                let removed =
                    parent.OutgoingEdges
                    |> Seq.map (fun kvp -> kvp.Key, kvp.Value.Remove block)
                    |> Seq.filter snd
                    |> Array.ofSeq
                assert(removed.Length = 1)
                let added = parent.OutgoingEdges[fst removed[0]].Add newBlock
                assert added
                let added = newBlock.IncomingCFGEdges.Add parent
                assert added
            block.IncomingCFGEdges.Clear()
            let added = block.IncomingCFGEdges.Add newBlock
            assert added
            newBlock.OutgoingEdges.Add(CfgInfo.TerminalForCFGEdge, HashSet[|block|])
            block

        let makeNewBasicBlock startVertex =
            match vertexToBasicBlock[int startVertex] with
            | None ->
                let newBasicBlock = BasicBlock(method, startVertex)
                vertexToBasicBlock[int startVertex] <- Some newBasicBlock
                addBasicBlock newBasicBlock
                newBasicBlock
            | Some block ->
                if block.StartOffset = startVertex then block
                else splitBasicBlock block startVertex

        let addEdge (src : BasicBlock) (dst : BasicBlock) =
            let added = dst.IncomingCFGEdges.Add src
            assert added
            let exists, edges = src.OutgoingEdges.TryGetValue CfgInfo.TerminalForCFGEdge
            if exists then
                let added = edges.Add dst
                assert added
            else
                src.OutgoingEdges.Add(CfgInfo.TerminalForCFGEdge, HashSet [|dst|])

        let rec dfs' (currentBasicBlock : BasicBlock) (currentVertex : offset) k =
            if used.Contains currentVertex then
                let existingBasicBlock = vertexToBasicBlock[int currentVertex]
                if currentBasicBlock <> existingBasicBlock.Value then
                    currentBasicBlock.FinalOffset <- findFinalVertex currentVertex currentBasicBlock
                    addEdge currentBasicBlock existingBasicBlock.Value
                if greyVertices.Contains currentVertex then
                    loopEntries.Add currentVertex |> ignore
                k ()
            else
                vertexToBasicBlock[int currentVertex] <- Some currentBasicBlock
                let added = greyVertices.Add currentVertex
                assert added
                let added = used.Add currentVertex
                assert added
                let instr = method.OffsetToInstr[currentVertex]
                let opCode = instr.opcode

                let dealWithJump srcBasicBlock dst k =
                    let newBasicBlock = makeNewBasicBlock dst
                    addEdge srcBasicBlock newBasicBlock
                    dfs' newBasicBlock dst k

                let processCall callFrom returnTo k =
                    currentBasicBlock.FinalOffset <- callFrom
                    let newBasicBlock = makeNewBasicBlock returnTo
                    addEdge currentBasicBlock newBasicBlock
                    dfs' newBasicBlock returnTo k

                let ipTransition = MethodBody.findNextInstructionOffsetAndEdges opCode ilBytes currentVertex

                let k _ =
                    let removed = greyVertices.Remove currentVertex
                    assert removed
                    k ()

                match ipTransition with
                | FallThrough offset when MethodBody.isDemandingCallOpCode opCode ->
                    let opCode', _ = MethodWithBody.parseCallSite method.MethodBase method.Bytecode currentVertex
                    assert (opCode' = opCode)
                    processCall  currentVertex offset k
                | FallThrough offset ->
                    currentBasicBlock.FinalOffset <- offset
                    dfs' currentBasicBlock offset k
                | ExceptionMechanism ->
                    currentBasicBlock.FinalOffset <- currentVertex
                    k ()
                | Return ->
                    sinks.Add currentBasicBlock
                    currentBasicBlock.FinalOffset <- currentVertex
                    k ()
                | UnconditionalBranch target ->
                    currentBasicBlock.FinalOffset <- currentVertex
                    dealWithJump currentBasicBlock target k
                | ConditionalBranch (fallThrough, offsets) ->
                    currentBasicBlock.FinalOffset <- currentVertex
                    let iterator _ dst k =
                        dealWithJump currentBasicBlock dst k
                    let destinations = HashSet(fallThrough :: offsets)
                    TestExecutor.Utils.Cps.foldlk iterator () destinations k

        startVertices
        |> Array.iter (fun v -> dfs' (makeNewBasicBlock v) v id)

        methodSize <- 0

        let sorted = basicBlocks |> Seq.sortBy (fun b -> b.StartOffset)
        for bb in sorted do
            methodSize <- methodSize + bb.BlockSize
            sortedBasicBlocks.Add bb

    let resolveBasicBlockIndex offset =
        let rec binSearch (sortedOffsets : ResizeArray<BasicBlock>) offset l r =
            if l >= r then l
            else
                let mid = (l + r) / 2
                let midValue = sortedOffsets[mid].StartOffset
                let leftIsLefter = midValue <= offset
                let rightIsRighter = mid + 1 >= sortedOffsets.Count || sortedOffsets[mid + 1].StartOffset > offset
                if leftIsLefter && rightIsRighter then mid
                elif not rightIsRighter
                    then binSearch sortedOffsets offset (mid + 1) r
                    else binSearch sortedOffsets offset l (mid - 1)

        binSearch sortedBasicBlocks offset 0 (sortedBasicBlocks.Count - 1)

    let resolveBasicBlock offset = sortedBasicBlocks[resolveBasicBlockIndex offset]

    do
        let startVertices =
            [|
             yield 0
             for handler in exceptionHandlers do
                 yield handler.handlerOffset
                 match handler.ehcType with
                 | ehcType.Filter offset -> yield offset
                 | _ -> ()
            |]

        dfs startVertices

    static member TerminalForCFGEdge = 0<terminalSymbol>
    member this.MethodBase = method.MethodBase
    member this.SortedBasicBlocks = sortedBasicBlocks
    member this.IlBytes = ilBytes
    member this.EntryPoint = sortedBasicBlocks[0]
    member this.MethodSize = methodSize
    member this.Sinks = sinks
    member this.IsLoopEntry offset = loopEntries.Contains offset
    member this.ResolveBasicBlock offset = resolveBasicBlock offset
    member this.IsBasicBlockStart offset = (resolveBasicBlock offset).StartOffset = offset
    // Returns dictionary of shortest distances, in terms of basic blocks (1 step = 1 basic block transition)
    member this.HasSiblings offset =
        let basicBlock = resolveBasicBlock offset
        basicBlock.HasSiblings

and Method internal (m : MethodBase) as this =
    let bytecode = lazy m.GetMethodBody().GetILAsByteArray()
    let instructions = lazy(
        let bytecode = bytecode.Value
        MethodInstrBuilder.build bytecode)
    let ehClauses = lazy(
        let body = m.GetMethodBody()
        let ehcs = body.ExceptionHandlingClauses
        let ehcsArray = Array.zeroCreate ehcs.Count
        for i in 0..ehcsArray.Length do
            ehcsArray[i] = ehClause.Create(ehcs[i]) |> ignore
        ehcsArray)
    let cfg = lazy(CfgInfo this)
    
    member x.MethodBase = m
    member x.Bytecode : byte array = bytecode.Value
    member x.Instructions : ResizeArray<ilInstr> = instructions.Value |> fst
    member x.OffsetToInstr : Dictionary<offset, ilInstr> = instructions.Value |> snd
    member x.EhClauses = ehClauses.Value
    member x.CFG with get() = cfg.Force()
    
    // Helps resolving cyclic dependencies between Application and MethodWithBody
    [<DefaultValue>] static val mutable private instantiator : MethodBase -> Method

    static member internal InstantiateNew with get() = Method.instantiator and set v = Method.instantiator <- v

    member x.BasicBlocks with get() =
        x.CFG.SortedBasicBlocks

    // Helps resolving cyclic dependencies between Application and MethodWithBody
    static member val internal CoverageZone : Method -> bool = fun _ -> true with get, set

    member x.InCoverageZone with get() = Method.CoverageZone x


    static member val internal AttributesZone : Method -> bool = fun _ -> true with get, set

    member x.CheckAttributes with get() = Method.AttributesZone x

    member x.BasicBlocksCount with get() = x.BasicBlocks.Count


module Application =
    let private methods = ConcurrentDictionary<methodDescriptor, Method>()

    let getMethod (m : MethodBase) : Method =
        let desc = getMethodDescriptor m
        Dict.getValueOrUpdate methods desc (fun () -> Method(m))

    do
        Method.InstantiateNew <- getMethod
