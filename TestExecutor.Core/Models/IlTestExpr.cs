using System.Text.Json;
using System.Text.Unicode;
using TestExecutor.Core.Visitors;

namespace TestExecutor.Core;


public class TypeRepr(string asm, int moduleToken, int typeToken, List<TypeRepr> genericArgs);

public class MethodRepr(TypeRepr declType, string signature, string name);

public class FieldRepr(TypeRepr type, string name);

public abstract class IlTestStmt
{
    public abstract StmtKind Kind { get; }
    public abstract TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor);
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
    public abstract TypeRepr? TypeRepr { get; set; }
}

public abstract class IlTestConst<T> : IlTestExpr
{
    public abstract T? Value { get; set; }
}

public class BoolConst() : IlTestConst<bool>
{
    public override StmtKind Kind { get; } = StmtKind.BOOL;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitBool(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
    public override bool Value { get; set; }
}

public class CharConst() : IlTestConst<char>
{
    public override StmtKind Kind { get; } = StmtKind.CHAR;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitChar(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
    public override char Value { get; set; }
}

public class Int8Const() : IlTestConst<sbyte>
{
    public override StmtKind Kind { get; } = StmtKind.INT8;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitInt8(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
    public override sbyte Value { get; set; }
}

public class Int16Const() : IlTestConst<short>
{
    public override StmtKind Kind { get; } = StmtKind.INT16;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitInt16(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
    public override short Value { get; set; }
}

public class Int32Const() : IlTestConst<int>
{
    public override StmtKind Kind { get; } = StmtKind.INT32;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitInt32(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
    public override int Value { get; set; }
}

public class Int64Const() : IlTestConst<long>
{
    public override StmtKind Kind { get; } = StmtKind.INT64;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitInt64(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
    public override long Value { get; set; }
}

public class UInt8Const() : IlTestConst<byte>
{
    public override StmtKind Kind { get; } = StmtKind.UINT8;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitUInt8(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
    public override byte Value { get; set; }
}

public class UInt16Const() : IlTestConst<ushort>
{
    public override StmtKind Kind { get; } = StmtKind.UINT16;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitUInt16(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
    public override ushort Value { get; set; }
}

public class UInt32Const() : IlTestConst<uint>
{
    public override StmtKind Kind { get; } = StmtKind.UINT32;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitUInt32(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
    public override uint Value { get; set; }
}

public class UInt64Const() : IlTestConst<ulong>
{
    public override StmtKind Kind { get; } = StmtKind.UINT64;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitUInt64(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
    public override ulong Value { get; set; }
}

public class FloatConst() : IlTestConst<float>
{
    public override StmtKind Kind { get; } = StmtKind.FLOAT;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitFloat(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
    public override float Value { get; set; }
}

public class DoubleConst() : IlTestConst<double>
{
    public override StmtKind Kind { get; } = StmtKind.DOUBLE;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitDouble(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
    public override double Value { get; set; }
}

public class StringConst : IlTestConst<string>
{
    public override StmtKind Kind { get; } = StmtKind.STRING;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitString(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
    public override string Value { get; set; }
}

public class NullConst(): IlTestExpr
{
    public override StmtKind Kind { get; } = StmtKind.INT8;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitNull(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
}

public class ArrayInstance() : IlTestExpr
{
    public override StmtKind Kind { get; } = StmtKind.NEW_ARRAY;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitArrayInstance(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
    public int Size { get; set; }
    public int Address { get; set; }
}

public class ObjectInstance() : IlTestExpr
{
    public override StmtKind Kind { get; } = StmtKind.NEW_OBJ;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitObjectInstance(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
    public int Address { get; set; }
}

public abstract class IlTestCall : IlTestExpr
{
    public abstract MethodRepr Method { get; set; }
    public abstract List<IlTestExpr> Args { get; }
}

public class InstanceMethodCall : IlTestCall
{
    public override StmtKind Kind { get; } = StmtKind.INSTANCE_CALL;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitInstanceMethodCall(this);
    }

    public override MethodRepr Method { get; set; }
    public override TypeRepr? TypeRepr { get; set; }
    public IlTestExpr Instance { get; set; }
    public override List<IlTestExpr> Args { get; } = [];
}

public class StaticMethodCall : IlTestCall
{
    public override StmtKind Kind { get; } = StmtKind.STATIC_CALL;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitStaticMethodCall(this);
    }

    public override MethodRepr Method { get; set; }
    public override TypeRepr? TypeRepr { get; set; }
    public override List<IlTestExpr> Args { get; } = [];
}

public class ConstructorCall : IlTestCall
{
    public override StmtKind Kind { get; } = StmtKind.CONSTRUCTOR_CALL;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitConstructorCall(this);
    }

    public override MethodRepr Method { get; set; }
    public override TypeRepr? TypeRepr { get; set; }
    public override List<IlTestExpr> Args { get; }
}

public abstract class ArrangeStmt : IlTestStmt
{
    public abstract IlTestExpr Instance { get; set; }
}

public class SetArrayIndex : ArrangeStmt
{
    public override StmtKind Kind { get; } = StmtKind.SET_ARRAY_INDEX;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitSetArrayIndex(this);
    }

    public override IlTestExpr Instance { get; set; }
    public int Index { get; set; }
    public IlTestExpr Value { get; set; }
}

public class SetObjectField : ArrangeStmt
{
    public override StmtKind Kind { get; } = StmtKind.SET_OBJ_FIELD;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitSetObjectField(this);
    }

    public override IlTestExpr Instance { get; set; }
    public FieldRepr Field { get; set; }
    public IlTestExpr Value { get; set; }
}

public class IlTypeInstance : IlTestExpr
{
    public override StmtKind Kind { get; } = StmtKind.TYPE_INSTANCE;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitIlTypeInstance(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
}

public class CyclicReference : IlTestExpr
{
    public override StmtKind Kind { get; } = StmtKind.CYCLIC_REFERENCE;
    public override TResult Accept<TResult>(ITestStmtVisitor<TResult> visitor)
    {
        return visitor.VisitCyclicRef(this);
    }

    public override TypeRepr? TypeRepr { get; set; }
}




