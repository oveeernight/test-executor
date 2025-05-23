using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using TestExpressions;
using Type = System.Type;

namespace TestExecutor.Core;

public record DispatchTestResult(MethodBase method, object?[] args, object? expectedValue);

public class TestResolver(IlTest test)
{
    private readonly Dictionary<int, object> _instances = new();

    private static readonly IList<MessageDescriptor> TestExpressionsDescriptors =
        TestExpressionsReflection.Descriptor.MessageTypes;

    public DispatchTestResult Resolve()
    {
        foreach (var arrangeStmt in test.ArrangeStmts)
        {
            var unpacked = UnpackAny(arrangeStmt);
            switch (unpacked)
            {
                case SetObjectField setField:
                    var objInstance = setField.Instance;
                    var resolvedObj = ResolveReferenceType(objInstance);
                    var value = InstantiateExpression(UnpackAny(setField.Value));
                    var field = ResolveField(objInstance.TypeRepr, setField.FieldRepr);
                    field.SetValue(resolvedObj, value);
                    break;
                case SetArrayIndex setArrayIndex:
                    var resolvedArr = (Array)ResolveReferenceType(setArrayIndex.Instance);
                    var index = setArrayIndex.Index;
                    var v = InstantiateExpression(UnpackAny(setArrayIndex.Value));
                    resolvedArr.SetValue(v, index);
                    break;
                default:
                    Console.Error.WriteLine($"Unexpected unpacked message: {unpacked}");
                    break;
            }
        }

        var methodCall = test.Call.Unpack<MethodCall>();
        var resolvedMethod = ResolveMethod(methodCall);
        var args = methodCall.Args.Select(UnpackAny).Select(InstantiateExpression).ToArray();
        var expectedValue = InstantiateExpression(UnpackAny(test.ExpectedResult));
        _instances.Clear();
        return new DispatchTestResult(resolvedMethod, args, expectedValue);
    }

    private object? InstantiateExpression(IMessage message)
    {
        switch (message)
        {
            case BoolConst c: return c.Value;
            case CharConst c: return Convert.ToChar(c.Value);
            case Int8Const c: return (sbyte)c.Value;
            case Int16Const c: return (short)c.Value;
            case Int32Const c: return c.Value;
            case Int64Const c: return c.Value;
            case UInt8Const c: return (byte)c.Value;
            case UInt16Const c: return (ushort)c.Value;
            case UInt32Const c: return (uint)c.Value;
            case UInt64Const c: return (ulong)c.Value;
            case FloatConst c: return c.Value;
            case DoubleConst c: return c.Value;
            case StringConst c: return c.Value;
            case NullConst: return null;
            case Combine c:
                var type = ResolveType(c.SightType);
                var slices = c.Slices.Select(slice =>
                {
                    var expr = InstantiateExpression(UnpackAny(slice.Expr));
                    return (expr, slice.Start, slice.End, slice.Pos);
                }).ToArray();
                return UnsafeUtils.ReinterpretCombine(type, slices);
            case ArrayInstance array: return ResolveReferenceType(array);
            case ObjectInstance obj: return ResolveReferenceType(obj);
            case IlTypeInstance ilTypeInstance: return ResolveType(ilTypeInstance.Type);
            default:
                Console.Error.WriteLine($"Instantiate expression: unexpected type {message.GetType()}");
                return null;
        }
    }

    private object ResolveReferenceType(IMessage message)
    {
        switch (message)
        {
            case ArrayInstance arrayInstance:
                var arrayAddress = arrayInstance.Address;
                if (_instances.TryGetValue(arrayAddress, out var array)) return array;
                var elementType = ResolveType(arrayInstance.ElementTypeRepr);
                var arrayType = elementType.MakeArrayType();
                var size = arrayInstance.Size;
                var newArray = Activator.CreateInstance(arrayType, size);
                _instances.Add(arrayAddress, newArray);
                return newArray;
            case ObjectInstance objectInstance:
                var address = objectInstance.Address;
                if (_instances.TryGetValue(address, out var obj)) return obj;
                var type = ResolveType(objectInstance.TypeRepr);
                var newObj = Activator.CreateInstance(type);
                _instances.Add(address, newObj);
                return newObj;
            default:
                Console.Error.WriteLine($"Resolve reference type: unexpected message {message}");
                return null;
        }
    }

    private static MethodInfo ResolveMethod(MethodCall methodCall)
    {
        var declTypeRepr = methodCall.MethodRepr.DeclType;
        var methodRepr = methodCall.MethodRepr;

        var declType = ResolveType(declTypeRepr);
        var methodInfo = declType.GetMethod(methodRepr.Name);
        if (methodInfo == null)
        {
            Console.Error.WriteLine($"Method {methodRepr.Name} not found");
        }

        return methodInfo!;
    }

    private static FieldInfo ResolveField(TypeRepr declTypeRepr, FieldRepr fieldRepr)
    {
        const BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var declType = ResolveType(declTypeRepr);
        var fieldType = ResolveType(fieldRepr.TypeRepr);
        var field = declType.GetFields(bindingFlags).FirstOrDefault(f => f.FieldType == fieldType && f.Name == fieldRepr.Name);
        if (field == null)
        {
            Console.Error.WriteLine($"Field {declType.Name}.{fieldRepr.Name} not found");
        }

        return field!;
    }

    private static Type ResolveType(TypeRepr typeRepr)
    {
        var asmName = typeRepr.Asm;
        Type[] types;
        if (TypesCache.TryGetValue(asmName, out var existingTypes))
        {
            types = existingTypes;
        }
        else
        {
            var asm = Assembly.Load(asmName);
            types = asm.Modules.First(m => m.MetadataToken == typeRepr.ModuleToken).GetTypes();
            TypesCache.Add(asmName, types);
        }

        var t = types.FirstOrDefault(t => t.FullName == typeRepr.FullName);
        if (t == null)
        {
            throw new Exception( $"Type {typeRepr} was not found among {string.Join(",", types.Select(typ => typ.FullName))}");
        }

        if (t.IsGenericType)
        {
            var resolvedGenerics = typeRepr.GenericArgs.Select(ResolveType).ToArray();
            t = t.MakeGenericType(resolvedGenerics);
        }

        return t;
    }

    public static MethodBase ResolveBatchExploredMethod(IlTestBatch batch)
    {
        var test = batch.Tests[0];
        var methodInfo = ResolveMethod(test.Call.Unpack<MethodCall>());
        return methodInfo;
    }

    private static IMessage UnpackAny(Any any)
    {
        var url = any.TypeUrl;
        var fullName = url.Substring("type.googleapis.com/".Length);
        var matchingDescriptor = TestExpressionsDescriptors.FirstOrDefault(descriptor => fullName == descriptor.FullName);
        if (matchingDescriptor == null)
        {
            throw new Exception($"Unable to find any matching descriptor for {url}");
        }

        return matchingDescriptor.Parser.ParseFrom(any.Value);
    }

    private static readonly Dictionary<string, Type[]> TypesCache = new();
}