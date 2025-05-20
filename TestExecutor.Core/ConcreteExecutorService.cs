using System.Reflection;
using Cfg;
using Grpc.Core;
using TestExpressions;
using TestExecutor.CoverageTool;

namespace TestExecutor.Core;

public class ConcreteExecutorService : ConcreteExecutor.ConcreteExecutorBase
{
    public Assembly? SamplesAssembly { get; set; }
    public override Task<ExecutionResult> Execute(IlTestBatch requestBatch, ServerCallContext context)
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        var mb = TestResolver.ResolveBatchExploredMethod(requestBatch);
        var method = Application.getMethod(mb);
        var cfg = method.CFG;

        var coverageTool = new InteractionCoverageTool(dir);
        var results = requestBatch.Tests.ToArray().Select<IlTest, ExecutionResult>(test =>
        {
            var resolver = new TestResolver(test);
            Exception? expectedException = null;
            try
            {
                var (method, callArgs, expected) = resolver.Resolve();

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

                coverageTool.SetEntryMain(method.Module.Assembly, method.Module.FullyQualifiedName, method.MetadataToken);
                coverageTool.SetCurrentThreadId(0);

                var actual = method.Invoke(instance, callArgs);

                var resultsEqual = ObjectsComparer.Equals(expected, actual);
                if (resultsEqual)
                {
                    return new ExecutionResult { Success = new Success() };
                }
                var reason = $"Symbolic result is not equal to actual, expected {expected}, actual {actual}";
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
                var reason = "Internal error occured:\n" + e.ToString();
                return new ExecutionResult { Fail = new Fail { Reason = reason } };
            }
        });
        
        var failedTests = results.Where(test => test.Fail is not null).ToArray();
        if (failedTests.Any())
        {
            var failReason = string.Join("\n", failedTests.Select(test => test.Fail.Reason));
            var result = new ExecutionResult { Fail = new Fail { Reason = failReason } };
            return Task.FromResult(result);
        }


        var (actualCoverage, info) = coverageTool.ComputeCoverage(cfg.MethodBase, cfg);

        if (CheckCoverage(cfg.MethodBase, actualCoverage))
        {
            var result = new ExecutionResult { Success = new Success() };
            return Task.FromResult(result);
        }
        else
        {
            info = $"Actual coverage is {actualCoverage}\n{info}";
            var result = new ExecutionResult { Fail = new Fail {Reason = info} };
            return Task.FromResult(result);
        }

    }
    
    private bool CheckCoverage(MethodBase method, int actual)
    {
        int x = 1;
        byte y = 1;
        var z = x + y;
        Type testSvmAttribute = SamplesAssembly.GetTypes().First(t => t.Name.EndsWith("SvmTestAttribute"));
        var coverageAttribute = method.GetCustomAttribute(testSvmAttribute);
        if (coverageAttribute == null)
        {
            throw new Exception($"Method {method.Name} does not have expected SvmTestAttribute");
        }
        var expectedCoverage = testSvmAttribute.GetProperty("ExpectedCoverage")?.GetValue(coverageAttribute);
        if (expectedCoverage == null)
        {
            throw new Exception($"Method {method.Name} does not have expected coverage attribute set");
        }
        
        return actual == (int)expectedCoverage;
    }
}