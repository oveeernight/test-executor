using System.Reflection;
using Google.Protobuf;
using TestExecutor.Core;

if (args[0] != "--src")
{
    Console.Error.WriteLine($"Unexpected flag {args[0]}");
    return -1;
}
var asm = args[1];

if (args[2] != "--test")
{
    Console.Error.WriteLine($"Unexpected flag {args[2]}");
    return -1;
}

var testFile = args[3];

var assembly = Assembly.LoadFrom(asm);
var types = assembly.GetTypes();
Console.WriteLine($"Found following types in assembly {assembly.FullName}: {string.Join(',', types.Select(t => t.FullName))}\n");

IlTest test;
var serializedTest = File.ReadAllBytes(testFile);

test = IlTest.Parser.ParseFrom(serializedTest);

// Console.WriteLine(test.ToString());

var resolver = new TestResolver();

var (method, callArgs, expected, expectedSerialized)= resolver.Resolve(test);

object? instance;

if (method.IsStatic)
{
    instance = null;
}
else
{
    instance = callArgs[0];
    callArgs = callArgs.Skip(1).ToArray();
}

var concreteResult = method.Invoke(instance, callArgs);

var concreteSerializedBytes = expectedSerialized.ToByteArray();
var expectedSerializedBytes = expectedSerialized.ToByteArray();
var resultsEqual = Equals(expectedSerializedBytes, concreteSerializedBytes);



Console.WriteLine(resultsEqual ? "it sucks" : "it's ok");
Console.WriteLine($"concrete result: {concreteResult}, expected: {expected}");

return 0;
