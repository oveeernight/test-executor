using System.Reflection;
using TestExecutor.Core;
using TestExpressions;




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


var serializedTest = File.ReadAllBytes(testFile);

var test = IlTest.Parser.ParseFrom(serializedTest);

// Console.WriteLine(test.ToString());

var resolver = new TestResolver();

var (method, callArgs, expected) = resolver.Resolve(test);

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

var actual = method.Invoke(instance, callArgs);

var resultsEqual = ObjectsComparer.Equals(expected, actual);


Console.WriteLine(resultsEqual ? "Results are equal" : "Results are not equal");
Console.WriteLine($"concrete result: {actual}, expected: {expected}");

return 0;
