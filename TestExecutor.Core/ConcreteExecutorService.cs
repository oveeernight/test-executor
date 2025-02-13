using System.Reflection;
using Grpc.Core;
using TestExpressions;

namespace TestExecutor.Core;

public class ConcreteExecutorService : ConcreteExecutor.ConcreteExecutorBase
{
    public override Task<ExecutionResult> Execute(IlTest request, ServerCallContext context)
    {
        var resolver = new TestResolver();
        Exception? expectedException = null;
        try
        {
            var (method, callArgs, expected) = resolver.Resolve(request);

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

            var actual = method.Invoke(instance, callArgs);

            var resultsEqual = ObjectsComparer.Equals(expected, actual);
            if (resultsEqual)
            {
                var result = new ExecutionResult { Success = new Success() };
                return Task.FromResult(result);
            }
            else
            {
                var reason = "Symbolic result is not equal to actual";
                var result = new ExecutionResult { Fail = new Fail { Reason = reason } };
                return Task.FromResult(result);
            }
        }
        catch (TargetInvocationException ex)
        {
            var actualException = ex.InnerException;
            if (ObjectsComparer.Equals(actualException, expectedException))
            {
                var result = new ExecutionResult { Success = new Success() };
                return Task.FromResult(result);
            }
            else
            {
                var reason = $"Unexpected exception {actualException}";
                var result = new ExecutionResult { Fail = new Fail { Reason = reason } };
                return Task.FromResult(result);
            }
        }
        catch (Exception e)
        {
            var reason = "Internal error occured:\n" + e.StackTrace!;
                var result = new ExecutionResult { Fail = new Fail { Reason = reason } };
                return Task.FromResult(result);
        }
    }
}