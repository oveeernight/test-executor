using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Type = System.Type;

namespace TestExecutor.Core;

public record DispatchTestResult(MethodBase method, object?[] args, object? expectedValue, IMessage? expectedValueSerialized);

public class TestResolver
{
    // private Type[] _sourceTypes = LoadAllTypes(Assembly.LoadFrom(sourceAsm));
    private Dictionary<int, object> _instances = new();
    private static IList<MessageDescriptor> _testExpressionsDescriptors = TestExpressionsReflection.Descriptor.MessageTypes;

    public DispatchTestResult Resolve(IlTest test)
    {
        foreach (var arrangeStmt in test.ArrangeStmts)
        {
            var unpacked = UnpackAny(arrangeStmt);
            Console.WriteLine($"unpacked message: {unpacked}");
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
            }
        }

        var methodCall = test.Call.Unpack<MethodCall>();
        Console.WriteLine($"methodCall: {methodCall}");
        var resolvedMethod = ResolveMethod(methodCall);
        var args = methodCall.Args.Select(UnpackAny).Select(InstantiateExpression).ToArray();
        var expectedValueSerialized = UnpackAny(test.ExpectedResult);
        var expectedValue = InstantiateExpression(UnpackAny(test.ExpectedResult));
        return new DispatchTestResult(resolvedMethod!, args, expectedValue, expectedValueSerialized);
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
            case ArrayInstance array: return ResolveReferenceType(array);
            case ObjectInstance obj: return ResolveReferenceType(obj);
            case CyclicReference cyclicReference: return ResolveReferenceType(cyclicReference);
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
            case CyclicReference cyclicReference:
                return _instances[cyclicReference.Address];
            default:
                Console.Error.WriteLine($"Resolve reference type: unexpected message {message}");
                return null;
        }
    }

    private MethodInfo ResolveMethod(MethodCall methodCall)
    {
        var declTypeRepr = methodCall.MethodRepr.DeclType;
        var methodRepr = methodCall.MethodRepr;

        var type =  ResolveType(declTypeRepr);
        // Console.WriteLine($"METHODS:");
        // Console.WriteLine(type.GetMethods().Select(m => m.Name).Aggregate((a, b) => $"{a}, {b}"));
        var methodInfo = type.GetMethod(methodRepr.Name);
        if (methodInfo == null)
        {
            Console.Error.WriteLine($"Method {methodRepr.Name} not found");
        }
        return methodInfo!;
    }

    private static FieldInfo ResolveField(TypeRepr declTypeRepr, FieldRepr fieldRepr)
    {
        var declType = ResolveType(declTypeRepr);
        var fieldType = ResolveType(fieldRepr.TypeRepr);
        var field = declType.GetFields().FirstOrDefault(f => f.FieldType == fieldType && f.Name == fieldRepr.Name);
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
        if (_typesCache.TryGetValue(asmName, out var existingTypes))
        {
            types = existingTypes;
        }
        else
        {
            var asm = Assembly.Load(asmName);
            types = asm.GetTypes();
            _typesCache.Add(asmName, types);
        }
        // foreach (var (k, v) in TestResolver._typesCache)
        // {
        //     Console.WriteLine($"{k} types: {string.Join(',', v.Select(t => t.FullName))}");
        // }

        // Console.WriteLine($"types: {string.Join(',', types.Select(t => t.FullName))}");
        var t = types.FirstOrDefault(t => t.MetadataToken == typeRepr.TypeToken);
        if (t == null)
        {
            Console.Error.WriteLine($"Type {typeRepr} was not found between {string.Join(",,,", types.Select(t => t.FullName))}");
        }
        return t!;
    }

    private static IMessage UnpackAny(Any any)
    {
        var url = any.TypeUrl;
        var matchingDescriptor = _testExpressionsDescriptors.FirstOrDefault(descriptor => url.EndsWith(descriptor.Name));
        if (matchingDescriptor == null)
        {
            Console.Error.WriteLine($"Unable to find any matching descroptor for {url}");
        }

        return matchingDescriptor!.Parser.ParseFrom(any.Value);
    }

    private static Type[] LoadAllTypes(Assembly assembly)
    {
        var result = new List<Type>();
        result.AddRange(assembly.GetTypes());
        var referenced = assembly.GetReferencedAssemblies();
        foreach (var referencedAssembly in referenced.Select(Assembly.Load))
        {
            result.AddRange(referencedAssembly.GetTypes());
        }

        return result.ToArray();
    }
    
    public static Dictionary<string, Type[]> _typesCache = new();
}