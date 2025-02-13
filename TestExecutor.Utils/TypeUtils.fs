module TestExecutor.Utils.TypeUtils

open System
open System.Reflection
open TestExecutor

let rec isReferenceTypeParameter (t : Type) =
    let checkAttribute (t : Type) =
        t.GenericParameterAttributes &&& GenericParameterAttributes.ReferenceTypeConstraint <> GenericParameterAttributes.None
    let isSimpleReferenceConstraint (t : Type) = t.IsClass && t <> typeof<ValueType>
    let isReferenceConstraint (c : Type) = if c.IsGenericParameter then isReferenceTypeParameter c else isSimpleReferenceConstraint c
    checkAttribute t || t.GetGenericParameterConstraints() |> Array.exists isReferenceConstraint

let isNullable = function
    | (t : Type) when t.IsGenericParameter ->
        if isReferenceTypeParameter t then false
        else __insufficientInformation__ "Can't determine if %O is a nullable type or not!" t
    | t -> Nullable.GetUnderlyingType(t) <> null