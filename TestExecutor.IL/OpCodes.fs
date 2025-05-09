module internal TestExecutor.IL.OpCodes

open System.Reflection
open System.Reflection.Emit

exception IncorrectCIL of string

let private equalSizeOpCodesCount = 0x100

let isSingleByteOpCodeValue (opCode : OpCode) = opCode.Size = 1
let singleByteOpCodes = Array.create equalSizeOpCodesCount OpCodes.Nop;
let isSingleByteOpCode = (<>) OpCodes.Prefix1.Value
let twoBytesOpCodes = Array.create equalSizeOpCodesCount OpCodes.Nop

do
    // Filling opcodes
    let (&&&) = Microsoft.FSharp.Core.Operators.(&&&)
    for field in typeof<OpCodes>.GetRuntimeFields() do
        match field.GetValue() with
        | :? OpCode as opCode ->
            let value = int opCode.Value
            if isSingleByteOpCodeValue opCode then singleByteOpCodes[value] <- opCode
            else twoBytesOpCodes[value &&& 0xFF] <- opCode
        | _ -> ()


let getOpCode (ilBytes : byte[]) (offset : int) =
    let offset = int offset
    let b1 = int16 ilBytes[offset]
    if isSingleByteOpCode b1 then singleByteOpCodes[int b1]
    elif offset + 1 >= ilBytes.Length then raise (IncorrectCIL("Prefix instruction FE without suffix!"))
    else twoBytesOpCodes[int ilBytes[offset + 1]]