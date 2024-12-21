namespace TestExecutor.Core.Visitors;

public interface ITestStmtVisitor<out TResult>
{
    TResult VisitBool(BoolConst stmt);
    TResult VisitChar(CharConst stmt);
    TResult VisitInt8(Int8Const stmt);
    TResult VisitInt16(Int16Const stmt);
    TResult VisitInt32(Int32Const stmt);
    TResult VisitInt64(Int64Const stmt);
    TResult VisitUInt8(UInt8Const stmt);
    TResult VisitUInt16(UInt16Const stmt);
    TResult VisitUInt32(UInt32Const stmt);
    TResult VisitUInt64(UInt64Const stmt);
    TResult VisitFloat(FloatConst stmt);
    TResult VisitDouble(DoubleConst stmt);
    TResult VisitString(StringConst stmt);
    TResult VisitNull(NullConst stmt);
    TResult VisitMethodCall(MethodCall stmt);
    TResult VisitArrayInstance(ArrayInstance stmt);
    TResult VisitObjectInstance(ObjectInstance stmt);
    TResult VisitSetArrayIndex(SetArrayIndex stmt);
    TResult VisitSetObjectField(SetObjectField stmt);
    TResult VisitIlTypeInstance(IlTypeInstance stmt);
    TResult VisitCyclicRef(CyclicReference stmt);
    
    TResult VisitTypeRepr(TypeRepr typeRepr);
    TResult VisitMethodRepr(MethodRepr methodRepr);
    TResult VisitFieldRepr(FieldRepr fieldRepr);
}