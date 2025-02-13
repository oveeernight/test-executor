namespace TestExecutor.IL

open System.Reflection.Emit

type offset = int

type ilInstr = {
    opcode: OpCode
    offset: offset
}