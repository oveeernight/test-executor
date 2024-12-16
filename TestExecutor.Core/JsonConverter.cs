using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestExecutor.Core;

public class JsonConverter : JsonConverter<IlTestStmt>
{
    public override IlTestStmt? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var next = reader.Read();
        if (!next) throw new JsonException("Expected object, got end of string");
        var token = reader.TokenType;
        if (token != JsonTokenType.StartObject) throw new JsonException($"Expected start of the object, got {token}");
        var field = reader.GetString();
        if (field != "Kind") throw new JsonException($"First field is expected to be Kind, but was {field}");
        var kind = reader.GetInt32();
        if (Enum.IsDefined(typeof(StmtKind), kind))
        {
            throw new JsonException($"Unexpected kind {kind}");
        }
        var kindAsEnum = (StmtKind)kind;
        var stmt = CreateByKind(kindAsEnum);
        return stmt;
    }

    public override void Write(Utf8JsonWriter writer, IlTestStmt value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    private IlTestStmt CreateByKind(StmtKind kind)
    {
        switch (kind)
        {
            case StmtKind.BOOL: return new BoolConst();
            case StmtKind.CHAR: return new CharConst();
            case StmtKind.INT8: return new Int8Const();
            case StmtKind.INT16: return new Int16Const();
            case StmtKind.INT32: return new Int32Const();
            case StmtKind.INT64: return new Int64Const();
            case StmtKind.UINT8: return new UInt8Const();
            case StmtKind.UINT16: return new UInt16Const();
            case StmtKind.UINT32: return new UInt32Const();
            case StmtKind.UINT64: return new UInt64Const();
            case StmtKind.FLOAT: return new FloatConst();
            case StmtKind.DOUBLE: return new DoubleConst();
            case StmtKind.STRING: return new StringConst();
            case StmtKind.NULL: return new NullConst();
            case StmtKind.NEW_ARRAY: return new ArrayInstance();
            case StmtKind.NEW_OBJ: return new ObjectInstance();
            case StmtKind.INSTANCE_CALL: return new InstanceMethodCall();
            case StmtKind.STATIC_CALL: return new StaticMethodCall();
            case StmtKind.CONSTRUCTOR_CALL: return new ConstructorCall();
            case StmtKind.SET_OBJ_FIELD: return new SetObjectField();
            case StmtKind.SET_ARRAY_INDEX: return new SetArrayIndex();
            case StmtKind.TYPE_INSTANCE: return new IlTypeInstance();
            case StmtKind.CYCLIC_REFERENCE: return new CyclicReference();
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }
}