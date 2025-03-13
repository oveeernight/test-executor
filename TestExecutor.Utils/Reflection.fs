module TestExecutor.Utils.Reflection

open System
open System.Reflection
open TestExecutor

[<CustomEquality; CustomComparison>]
type methodDescriptor = {
    methodHandle : nativeint
    declaringTypeVarHandles : nativeint array
    methodVarHandles : nativeint array
    typeHandle : nativeint
}
with
    override x.GetHashCode() =
        HashCode.Combine(x.methodHandle, hash x.declaringTypeVarHandles, hash x.methodVarHandles, x.typeHandle)

    override x.Equals(another) =
        match another with
        | :? methodDescriptor as d -> (x :> IComparable).CompareTo d = 0
        | _ -> false

    interface IComparable with
        override x.CompareTo y =
            match y with
            | :? methodDescriptor as y ->
                compare
                    (x.methodHandle, x.declaringTypeVarHandles, x.methodVarHandles, x.typeHandle)
                    (y.methodHandle, y.declaringTypeVarHandles, y.methodVarHandles, y.typeHandle)
            | _ -> -1

    // ----------------------------- Binding Flags ------------------------------

let staticBindingFlags =
    let (|||) = Microsoft.FSharp.Core.Operators.(|||)
    BindingFlags.Static ||| BindingFlags.NonPublic ||| BindingFlags.Public
let instanceBindingFlags =
    let (|||) = Microsoft.FSharp.Core.Operators.(|||)
    BindingFlags.Instance ||| BindingFlags.NonPublic ||| BindingFlags.Public
let instanceNonPublicBindingFlags =
    let (|||) = Microsoft.FSharp.Core.Operators.(|||)
    BindingFlags.Instance ||| BindingFlags.NonPublic
let instancePublicBindingFlags =
    let (|||) = Microsoft.FSharp.Core.Operators.(|||)
    BindingFlags.Instance ||| BindingFlags.Public
let allBindingFlags =
    let (|||) = Microsoft.FSharp.Core.Operators.(|||)
    staticBindingFlags ||| instanceBindingFlags

let methodIsDynamic (m : MethodBase) =
    m.DeclaringType = null
            
let getMethodDescriptor (m : MethodBase) =
    let reflectedType = m.ReflectedType
    let typeHandle =
        if reflectedType <> null then reflectedType.TypeHandle.Value
        else IntPtr.Zero
    let declaringTypeVars =
        if reflectedType <> null && reflectedType.IsGenericType then
            reflectedType.GetGenericArguments() |> Array.map (fun t -> t.TypeHandle.Value)
        else [||]
    let methodVars =
        if m.IsGenericMethod then m.GetGenericArguments() |> Array.map (fun t -> t.TypeHandle.Value)
        else [||]
    let methodHandle =
        if methodIsDynamic m then m.GetHashCode() |> nativeint
        else m.MethodHandle.Value
    {
        methodHandle = methodHandle
        declaringTypeVarHandles = declaringTypeVars
        methodVarHandles = methodVars
        typeHandle = typeHandle
    }

let getMethodReturnType : MethodBase -> Type = function
    | :? ConstructorInfo -> typeof<Void>
    | :? MethodInfo as m -> m.ReturnType
    | _ -> internalfail "unknown MethodBase"

let hasNonVoidResult m =
    getMethodReturnType m <> typeof<Void> && not m.IsConstructor

let hasThis (m : MethodBase) = m.CallingConvention.HasFlag(CallingConventions.HasThis)

let getFullTypeName (typ : Type) =
    if typ <> null then typ.ToString() else String.Empty

let getFullMethodName (methodBase : MethodBase) =
    let returnType = getMethodReturnType methodBase |> getFullTypeName
    let declaringType = getFullTypeName methodBase.DeclaringType
    let parameters =
        methodBase.GetParameters()
        |> Seq.map (fun param -> getFullTypeName param.ParameterType)
        |> if methodBase.IsStatic then id else ((fun x xs ->  seq { yield x; yield! xs }) "this")
        |> join ", "
//        let typeParams =
//            if not methodBase.IsGenericMethod then ""
//            else methodBase.GetGenericArguments() |> Seq.map getFullTypeName |> join ", " |> sprintf "[%s]"
    sprintf "%s %s.%s(%s)" returnType declaringType methodBase.Name parameters
    
let private retrieveMethodsGenerics (method : MethodBase) =
    match method with
    | :? MethodInfo as mi -> mi.GetGenericArguments()
    | :? ConstructorInfo -> null
    | _ -> __notImplemented__()
    
let resolveMethod (method : MethodBase) methodToken =
    let typGenerics = method.DeclaringType.GetGenericArguments()
    let methodGenerics = retrieveMethodsGenerics method
    method.Module.ResolveMethod(methodToken, typGenerics, methodGenerics)
    
    // ----------------------------------- Creating objects ----------------------------------

let defaultOf (t : Type) =
    assert(not t.IsByRefLike)
    if t.IsValueType && Nullable.GetUnderlyingType(t) = null && not t.ContainsGenericParameters
        then Activator.CreateInstance t
        else null

let createObject (t : Type) =
    assert(not t.IsByRefLike)
    match t with
    | _ when t = typeof<String> -> String.Empty :> obj
    | _ when TypeUtils.isNullable t -> null
    | _ when t.IsArray -> Array.CreateInstance(typeof<obj>, 1)
    | _ when t.ContainsGenericParameters -> internalfail $"Creating object of open generic type {t}"
    | _ -> System.Runtime.Serialization.FormatterServices.GetUninitializedObject t