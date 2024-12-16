// using System.Text.Json;
//
// namespace TestExecutor.Core.Visitors;
//
// public ref struct SerializationContext
// {
//     public Utf8JsonReader reader;
// }
//
// ref struct DeserializingVisitor() : ITestStmtVisitor<IlTestStmt>
// {
//     public static SerializationContext SerializationContext { get; set; }
//
//     public IlTestStmt VisitBool(BoolConst stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitChar(CharConst stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitInt8(Int8Const stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitInt16(Int16Const stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitInt32(Int32Const stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitInt64(Int64Const stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitUInt8(UInt8Const stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitUInt16(UInt16Const stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitUInt32(UInt32Const stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitUInt64(UInt64Const stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitFloat(FloatConst stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitDouble(DoubleConst stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitString(StringConst stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitNull(NullConst stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitInstanceMethodCall(InstanceMethodCall stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitStaticMethodCall(StaticMethodCall stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitConstructorCall(ConstructorCall stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitArrayInstance(ArrayInstance stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitObjectInstance(ObjectInstance stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitSetArrayIndex(SetArrayIndex stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitSetObjectField(SetObjectField stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitIlTypeInstance(IlTypeInstance stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitCyclicRef(CyclicReference stmt)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitTypeRepr(TypeRepr typeRepr)
//     {
//         var next = ctx.reader.Read();
//     }
//
//     public IlTestStmt VisitMethodRepr(MethodRepr methodRepr)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IlTestStmt VisitFieldRepr(FieldRepr fieldRepr)
//     {
//         throw new NotImplementedException();
//     }
// }