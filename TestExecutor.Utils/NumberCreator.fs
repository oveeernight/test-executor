module TestExecutor.Utils.NumberCreator

open System


let public extractInt32 (ilBytes : byte []) (pos : int) =
    BitConverter.ToInt32(ilBytes, int pos)
let public extractOffset (ilBytes : byte []) (pos : int) : int =
    BitConverter.ToInt32(ilBytes, int pos)
let public extractUnsignedInt32 (ilBytes : byte []) (pos : int) =
    BitConverter.ToUInt32(ilBytes, int pos)
let public extractUnsignedInt16 (ilBytes : byte []) (pos : int) =
    BitConverter.ToUInt16(ilBytes, int pos)
let public extractInt64 (ilBytes : byte []) (pos : int) =
    BitConverter.ToInt64(ilBytes, int pos)
let public extractInt8 (ilBytes : byte []) (pos : int) =
    ilBytes[int pos] |> sbyte |> int
let public extractUnsignedInt8 (ilBytes : byte []) (pos : int) =
    ilBytes[int pos]
let public extractFloat64 (ilBytes : byte []) (pos : int) =
    BitConverter.ToDouble(ilBytes, int pos)
let public extractFloat32 (ilBytes : byte []) (pos : int) =
    BitConverter.ToSingle(ilBytes, int pos)