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
        
        var failedTestsReasons = new List<string>();
        var allTestsCount = requestBatch.Tests.Count;
        var reproduced = 0;
        var coverageTool = new InteractionCoverageTool(dir);
        foreach (var test in requestBatch.Tests)
        {
            var resolver = new TestResolver(test);
            Exception? expectedException = null;
            try
            {
                var (m, callArgs, expected) = resolver.Resolve();

                if (expected is Exception e)
                {
                    expectedException = e;
                }

                object? instance;

                if (m.IsStatic)
                {
                    instance = null;
                }
                else
                {
                    instance = callArgs[0];
                    callArgs = callArgs.Skip(1).ToArray();
                }

                coverageTool.SetEntryMain(m.Module.Assembly, m.Module.FullyQualifiedName, m.MetadataToken);
                coverageTool.SetCurrentThreadId(0);

                var actual = m.Invoke(instance, callArgs);

                var resultsEqual = ObjectsComparer.Equals(expected, actual);
                if (resultsEqual)
                {
                    reproduced++;
                }
                else
                {
                    var reason = $"Symbolic result is not equal to actual, expected {expected}, actual {actual}";
                    failedTestsReasons.Add(reason);
                }
            }
            catch (TargetInvocationException ex)
            {
                var actualException = ex.InnerException;
                if (actualException?.GetType() == expectedException?.GetType())
                // if (ObjectsComparer.Equals(actualException, expectedException))
                {
                    reproduced++;
                }
                else
                {
                    var reason = $"Unexpected exception {actualException}";
                    failedTestsReasons.Add(reason);   
                }
            }
            catch (Exception e)
            {
                var reason = "Internal error occured:\n" + e;
                failedTestsReasons.Add(reason);
            }
        }
        
        var (actualCoverage, info) = coverageTool.ComputeCoverage(cfg.MethodBase, cfg);
        
        if (failedTestsReasons.Any())
        {
            var reproducingFailReason = string.Join("\n", failedTestsReasons);
            var result = new ExecutionResult
            {
                Fail = new Fail { Reason = reproducingFailReason, Coverage = actualCoverage, Reproduced = reproduced }
            };
            return Task.FromResult(result);
        }

        if (CheckCoverage(cfg.MethodBase, actualCoverage))
        {
            var result = new ExecutionResult { Success = new Success { Coverage = actualCoverage, GeneratedTests = allTestsCount } };
            return Task.FromResult(result);
        }
        else
        {
            info = $"Actual coverage is {actualCoverage}\n{info}";
            var result = new ExecutionResult { Fail = new Fail {Reason = info, Coverage = actualCoverage, Reproduced = reproduced} };
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