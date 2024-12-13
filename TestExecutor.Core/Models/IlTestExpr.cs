namespace TestExecutor.Core;


public class TypeRepr(string asm, int moduleToken, int typeToken, List<TypeRepr> genericArgs);

public class MethodRepr(TypeRepr declType, string signature, string name);

public class FieldRepr(TypeRepr type, string name);

public abstract class IlTestStmt
{
    public abstract StmtKind Kind { get; }
}

public enum StmtKind
{
    BOOL, CHAR, INT8, INT16, INT32, INT64, UINT8, UINT16, UINT32, UINT64, FLOAT, DOUBLE,
    STRING, NULL,
    INSTANCE_CALL, STATIC_CALL, CONSTRUCTOR_CALL,
    NEW_OBJ, NEW_ARRAY,
    SET_OBJ_FIELD, SET_ARRAY_INDEX,
    TYPE_INSTANCE,
    CYCLIC_REFERENCE
}

public abstract class IlTestExpr : IlTestStmt
{
    public abstract TypeRepr? Type { get; set; }
}

public abstract class IlTestConst<T> : IlTestExpr
{
    public abstract T? Value { get; set; }
}

public class BoolConst(bool value, TypeRepr type) : IlTestConst<bool>
{
    public override StmtKind Kind { get; } = StmtKind.BOOL;
    public override TypeRepr? Type { get; set; } = type;
    public override bool Value { get; set; } = value;
}

public class CharConst(char value, TypeRepr type) : IlTestConst<char>
{
    public override StmtKind Kind { get; } = StmtKind.CHAR;
    public override TypeRepr? Type { get; set; } = type;
    public override char Value { get; set; } = value;
}

public class Int8Const() : IlTestConst<sbyte>
{
    public override StmtKind Kind { get; } = StmtKind.INT8;
    public override TypeRepr? Type { get; set; }
    public override sbyte Value { get; set; }
}

public class Int16Const() : IlTestConst<short>
{
    public override StmtKind Kind { get; } = StmtKind.INT16;
    public override TypeRepr? Type { get; set; }
    public override short Value { get; set; }
}

public class Int32Const(int value, TypeRepr type) : IlTestConst<int>
{
    public override StmtKind Kind { get; } = StmtKind.INT32;
    public override TypeRepr? Type { get; set; }
    public override int Value { get; set; }
}

public class Int64Const(long value, TypeRepr type) : IlTestConst<long>
{
    public override StmtKind Kind { get; } = StmtKind.INT64;
    public override TypeRepr? Type { get; set; }
    public override long Value { get; set; }
}

public class UInt8Const(byte value, TypeRepr type) : IlTestConst<byte>
{
    public override StmtKind Kind { get; } = StmtKind.UINT8;
    public override TypeRepr? Type { get; set; }
    public override byte Value { get; set; }
}

public class UInt16Const(ushort value, TypeRepr type) : IlTestConst<ushort>
{
    public override StmtKind Kind { get; } = StmtKind.UINT16;
    public override TypeRepr? Type { get; set; }
    public override ushort Value { get; set; }
}

public class UInt32Const(uint value, TypeRepr type) : IlTestConst<uint>
{
    public override StmtKind Kind { get; } = StmtKind.UINT32;
    public override TypeRepr? Type { get; set; }
    public override uint Value { get; set; }
}

public class UInt64Const(ulong value, TypeRepr type) : IlTestConst<ulong>
{
    public override StmtKind Kind { get; } = StmtKind.UINT64;
    public override TypeRepr? Type { get; set; }
    public override ulong Value { get; set; }
}

public class FloatConst(float value, TypeRepr type) : IlTestConst<float>
{
    public override StmtKind Kind { get; } = StmtKind.FLOAT;
    public override TypeRepr? Type { get; set; }
    public override float Value { get; set; }
}

public class DoubleConst(double value, TypeRepr type) : IlTestConst<double>
{
    public override StmtKind Kind { get; } = StmtKind.DOUBLE;
    public override TypeRepr? Type { get; set; }
    public override double Value { get; set; }
}

public class StringConst : IlTestConst<string>
{
    public override StmtKind Kind { get; } = StmtKind.STRING;
    public override TypeRepr? Type { get; set; }
    public override string Value { get; set; }
}

public class NullConst(TypeRepr type) : IlTestConst<object>
{
    public override StmtKind Kind { get; } = StmtKind.INT8;
    public override TypeRepr? Type { get; set; }
    public override object? Value { get; set; } = null;
}

public class ArrayInstance(TypeRepr type, int size, int address) : IlTestExpr
{
    public override StmtKind Kind { get; } = StmtKind.NEW_ARRAY;
    public override TypeRepr? Type { get; set; }
}

public class ObjectInstance(TypeRepr type, int address) : IlTestExpr
{
    public override StmtKind Kind { get; } = StmtKind.NEW_OBJ;
    public override TypeRepr? Type { get; set; }
}

public abstract class IlTestCall : IlTestExpr
{
    public abstract MethodRepr Method { get; set; }
    public abstract List<IlTestExpr> Args { get; }
}

public class InstanceMethodCall : IlTestCall
{
    public override StmtKind Kind { get; } = StmtKind.INSTANCE_CALL;
    public override MethodRepr Method { get; set; }
    public override TypeRepr? Type { get; set; }
    public IlTestExpr Instance { get; set; }
    public override List<IlTestExpr> Args { get; } = [];
}

public class StaticMethodCall : IlTestCall
{
    public override StmtKind Kind { get; } = StmtKind.STATIC_CALL;
    public override MethodRepr Method { get; set; }
    public override TypeRepr? Type { get; set; }
    public override List<IlTestExpr> Args { get; } = [];
}

public class ConstructorCall : IlTestCall
{
    public override StmtKind Kind { get; } = StmtKind.CONSTRUCTOR_CALL;
    public override MethodRepr Method { get; set; }
    public override TypeRepr? Type { get; set; }
    public override List<IlTestExpr> Args { get; }
}

public abstract class ArrangeStmt : IlTestStmt
{
    public abstract IlTestExpr Instance { get; set; }
}

public class SetArrayIndex : ArrangeStmt
{
    public override StmtKind Kind { get; } = StmtKind.SET_ARRAY_INDEX;
    public override IlTestExpr Instance { get; set; }
    public int Index { get; set; }
    public IlTestExpr Value { get; set; }
}

public class SetObjectField : ArrangeStmt
{
    public override StmtKind Kind { get; } = StmtKind.SET_OBJ_FIELD;
    public override IlTestExpr Instance { get; set; }
    public FieldRepr Field { get; set; }
    public IlTestExpr Value { get; set; }
}

public class IlTypeInstance : IlTestExpr
{
    public override StmtKind Kind { get; } = StmtKind.TYPE_INSTANCE;
    public override TypeRepr? Type { get; set; }
}

public class CyclicReference : IlTestExpr
{
    public override StmtKind Kind { get; } = StmtKind.CYCLIC_REFERENCE;
    public override TypeRepr? Type { get; set; }
}




