module TestExecutor.Utils.TypeUtils

open System
open System.Collections.Generic
open System.Reflection
open System.Reflection.Emit
open TestExecutor


module EnumUtils =

    /// <summary>
    /// Gets enum type underlying type throwing InsufficientInformationException if enum is a generic parameter.
    /// TODO: Implement default underlying types values (Int32) using assumptions.
    /// </summary>
    let getEnumUnderlyingTypeChecked (t : Type) =
        if not t.IsEnum then
            invalidArg "t" "Type must be enum"

        if t.IsGenericParameter then
            __insufficientInformation__ $"Cannot determine underlying type for generic enum type {t.Name}"

        t.GetEnumUnderlyingType()

    let getEnumDefaultValue (t : Type) =
        if not t.IsEnum then
            invalidArg "t" "Type must be enum"

        let value = Activator.CreateInstance t

        if t.IsEnumDefined value then
            value
        else
            let allValues = t.GetEnumValues()
            if allValues.Length <> 0 then
                allValues.GetValue(0)
            else
                value
      

let private integralTypes =
    HashSet<Type>(
        [
            typeof<byte>; typeof<sbyte>; typeof<int16>; typeof<uint16>
            typeof<int32>; typeof<uint32>; typeof<int64>; typeof<uint64>;
            typeof<char>; typeof<IntPtr>; typeof<UIntPtr>
        ]
    )      

let private realTypes = HashSet<Type>([typeof<single>; typeof<double>])
let private numericTypes = HashSet<Type>(Seq.append integralTypes realTypes)
let isNumeric x = numericTypes.Contains x || x.IsEnum

let private nativeSize = IntPtr.Size
let isNative x = x = typeof<IntPtr> || x = typeof<UIntPtr>



type private sizeOfType = Func<uint32>

let private sizeOfs = Dictionary<Type, sizeOfType>()

let private createSizeOf (typ : Type) =
    assert(not typ.ContainsGenericParameters && typ <> typeof<Void>)
    let m = DynamicMethod("GetManagedSizeImpl", typeof<uint32>, null);
    let gen = m.GetILGenerator()
    gen.Emit(OpCodes.Sizeof, typ)
    gen.Emit(OpCodes.Ret)
    m.CreateDelegate(typeof<sizeOfType>) :?> sizeOfType

let getSizeOf typ =
    let result : sizeOfType ref = ref null
    if sizeOfs.TryGetValue(typ, result) then result.Value
    else
        let sizeOf = createSizeOf typ
        sizeOfs.Add(typ, sizeOf)
        sizeOf

let numericSizeOf (typ : Type) : uint32 =
    let typ = if typ.IsEnum then EnumUtils.getEnumUnderlyingTypeChecked typ else typ
    assert(isNumeric typ)
    match typ with
    | _ when typ = typeof<int8> -> uint sizeof<int8>
    | _ when typ = typeof<uint8> -> uint sizeof<uint8>
    | _ when typ = typeof<int16> -> uint sizeof<int16>
    | _ when typ = typeof<uint16> -> uint sizeof<uint16>
    | _ when typ = typeof<char> -> uint sizeof<char>
    | _ when typ = typeof<int32> -> uint sizeof<int32>
    | _ when typ = typeof<uint32> -> uint sizeof<uint32>
    | _ when typ = typeof<float32> -> uint sizeof<float32>
    | _ when typ = typeof<int64> -> uint sizeof<int64>
    | _ when typ = typeof<uint64> -> uint sizeof<uint64>
    | _ when typ = typeof<float> -> uint sizeof<float>
    | _ when typ = typeof<IntPtr> -> uint nativeSize
    | _ when typ = typeof<UIntPtr> -> uint nativeSize
    | _ -> __unreachable__()

let internalSizeOf (typ : Type) : int32 =
    if isNative typ || typ.IsByRef || not typ.IsValueType then nativeSize
    elif isNumeric typ then numericSizeOf typ |> int
    elif typ.ContainsGenericParameters then
        __insufficientInformation__ $"SizeOf: cannot calculate size of generic type {typ}"
    else
        let sizeOf = getSizeOf(typ)
        sizeOf.Invoke() |> int

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
