namespace TestExecutor.Core.Models;

public class TestExecutorException
{
public int Abs(int x) {
    var result = x;
    if (x < 0) {
        result = -x;
    }
    if (result < 0) {
        throw new Exception();
    }
    return result;
}
    
}