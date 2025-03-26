namespace TestExecutor.CoverageTool


open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Reflection
open System.Runtime.InteropServices
open System.Text
open Cfg
open Microsoft.FSharp.NativeInterop
open TestExecutor.CoverageTool.CoverageDeserializer
open TestExecutor


#nowarn "9"

module private ExternalCalls =
    [<DllImport("libvsharpCoverage", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)>]
    extern void SetEntryMain(byte* assemblyName, int assemblyNameLength, byte* moduleName, int moduleNameLength, int methodToken)

    [<DllImport("libvsharpCoverage", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)>]
    extern void GetHistory(nativeint size, nativeint data)

    [<DllImport("libvsharpCoverage", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)>]
    extern void SetCurrentThreadId(int id)

type InteractionCoverageTool(workingDirectory: DirectoryInfo) =
    let mutable entryMainWasSet = false

    let castPtr ptr =
        NativePtr.toVoidPtr ptr |> NativePtr.ofVoidPtr

    let getCoverageInfo (allBlocks: ResizeArray<BasicBlock>) (visited: HashSet<BasicBlock>) =
        let sb = StringBuilder()
        let mutable allCovered = true
        for block in allBlocks do
            if visited.Contains block |> not then
                allCovered <- false
                sb.AppendLine $"Block [0x{block.StartOffset:X} .. 0x{block.FinalOffset:X}] not covered" |> ignore
                for inst in block.GetInstructions() do
                    sb.AppendLine $"{inst.opcode}" |> ignore
        if allCovered then
            sb.AppendLine "All blocks are covered" |> ignore
        sb.ToString()
        
        
    member private this.GetRawHistory () =
        if not entryMainWasSet then
            internalfail "Try call GetRawHistory, while entryMain wasn't set"
        let sizePtr = NativePtr.stackalloc<uint> 1
        let dataPtrPtr = NativePtr.stackalloc<nativeint> 1

        ExternalCalls.GetHistory(NativePtr.toNativeInt sizePtr, NativePtr.toNativeInt dataPtrPtr)

        let size = NativePtr.read sizePtr |> int
        let dataPtr = NativePtr.read dataPtrPtr

        let data = Array.zeroCreate<byte> size
        Marshal.Copy(dataPtr, data, 0, size)
        data

    member this.SetEntryMain (assembly : Assembly) (moduleName : string) (methodToken : int) =
        entryMainWasSet <- true
        let assemblyNamePtr = fixed assembly.FullName.ToCharArray()
        let moduleNamePtr = fixed moduleName.ToCharArray()
        let assemblyNameLength = assembly.FullName.Length
        let moduleNameLength = moduleName.Length

        ExternalCalls.SetEntryMain(
            castPtr assemblyNamePtr,
            assemblyNameLength,
            castPtr moduleNamePtr,
            moduleNameLength,
            methodToken
        )
    member this.ComputeCoverage (mb: MethodBase, cfg: CfgInfo) =

        let visitedBlocks = HashSet<BasicBlock>()
        let visited =
            this.GetRawHistory()
            |> getRawReports
            |> reportsFromRawReports
        
        let token = cfg.MethodBase.MetadataToken
        let moduleName = cfg.MethodBase.Module.FullyQualifiedName
        for coverageReport in visited do
            for loc in coverageReport.coverageLocations do
                // Filtering coverage records that are only relevant to this method
                if loc.methodToken = token && loc.moduleName = moduleName then
                    let offset = loc.offset
                    let block = cfg.ResolveBasicBlock offset
                    if block.FinalOffset = offset then
                        visitedBlocks.Add block |> ignore

        let coveredSize = visitedBlocks |> Seq.sumBy (_.BlockSize)
        (double coveredSize) / (double cfg.MethodSize) * 100. |> int, getCoverageInfo cfg.SortedBasicBlocks visitedBlocks
        
    member this.SetCurrentThreadId id =
        ExternalCalls.SetCurrentThreadId(id)
