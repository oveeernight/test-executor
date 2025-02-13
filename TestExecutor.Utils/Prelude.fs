namespace TestExecutor

open System


type InternalException(msg : string) =
    inherit Exception(msg)
    
type InsufficientInformationException(msg : string) =
    inherit Exception(msg)
    
type UnreachableException(msg : string) =
    inherit Exception(msg)

[<AutoOpen>]
module public Prelude =

    let public internalfail message = raise (InternalException message)
    let public __insufficientInformation__ format = Printf.ksprintf (fun reason -> InsufficientInformationException ("Insufficient information! " + reason) |> raise) format
    let inline public __unreachable__() = raise (UnreachableException "unreachable branch hit!")
    let inline public __notImplemented__() = raise (NotImplementedException())

    let inline public join s (ss : seq<string>) = String.Join(s, ss)
