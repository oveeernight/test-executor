using System.Reflection;
using Grpc.Core;
using TestExpressions;
using TestExecutor.CoverageTool;

namespace TestExecutor.Core;

public class ConcreteExecutorService(Assembly samplesAssembly) : ConcreteExecutor.ConcreteExecutorBase
{
    public override Task<ExecutionResult> Execute(IlTestBatch requestBatch, ServerCallContext context)
    {
        var resolver = new TestResolver();
        MethodBase? exploredMethod = null;
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        var coverageTool = new InteractionCoverageTool(dir);
        var results = requestBatch.Tests.ToArray().Select<IlTest, ExecutionResult>(test =>
        {
            Exception? expectedException = null;
            try
            {
                var (method, callArgs, expected) = resolver.Resolve(test);
                exploredMethod = method;

                if (expected is Exception e)
                {
                    expectedException = e;
                }

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

                coverageTool.SetEntryMain(method.Module.Assembly, method.Module.Name, method.MetadataToken);

                var actual = method.Invoke(instance, callArgs);

                var resultsEqual = ObjectsComparer.Equals(expected, actual);
                if (resultsEqual)
                {
                    return new ExecutionResult { Success = new Success() };
                }
                var reason = "Symbolic result is not equal to actual";
                return new ExecutionResult { Fail = new Fail { Reason = reason } };
            }
            catch (TargetInvocationException ex)
            {
                var actualException = ex.InnerException;
                if (ObjectsComparer.Equals(actualException, expectedException))
                {
                    return new ExecutionResult { Success = new Success() };
                }

                var reason = $"Unexpected exception {actualException}";
                return new ExecutionResult { Fail = new Fail { Reason = reason } };
            }
            catch (Exception e)
            {
                var reason = "Internal error occured:\n" + e.StackTrace!;
                return new ExecutionResult { Fail = new Fail { Reason = reason } };
            }
        });

        var (actualCoverage, info) = coverageTool.ComputeCoverage(exploredMethod);
        if (CheckCoverage(exploredMethod, actualCoverage))
        {
            var result = new ExecutionResult { Success = new Success() };
            return Task.FromResult(result);
        }
        else
        {
            var result = new ExecutionResult { Fail = new Fail {Reason = info} };
            return Task.FromResult(result);
        }

    }
    
    private Type coverageAttributeType = samplesAssembly.GetTypes().First(t => t.Name.EndsWith("ExpectedCoverageAttribute"));
    private bool CheckCoverage(MethodBase method, int actual)
    {
        var coverageAttribute = method.GetCustomAttribute(coverageAttributeType);
        if (coverageAttribute == null)
        {
            throw new Exception($"Method {method.Name} does not have expected coverage attribute");
        }
        var expectedCoverage = coverageAttributeType.GetProperty("ExpectedCoverage")?.GetValue(coverageAttribute);
        if (expectedCoverage == null)
        {
            throw new Exception($"Method {method.Name} does not have expected coverage attribute set");
        }
        
        return actual == (int)expectedCoverage;
    }
}