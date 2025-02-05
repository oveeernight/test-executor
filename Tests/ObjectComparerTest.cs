using TestExecutor.Core;

namespace Tests;

[TestFixture]
public class ObjectComparerTest
{
    [TestCaseSource(nameof(CompareCases))]
    public void Compare(object o1, object o2, bool expected)
    {
        Assert.AreEqual(expected, ObjectsComparer.Equals(o1, o2));
    }

   
    private class Node
    {
        public Node? next;
        public int value;
    }
    
    private static Node nodeStub = new ();
    
    public static object[] CompareCases =
    {
        new object[] { 1, 1, true },
        new object[] { 0, 0, true },
        new object[] { int.MaxValue, int.MaxValue, true },
        new object[] { int.MinValue, int.MinValue, true },
        new object[] { long.MaxValue, long.MaxValue, true },
        new object[] { long.MinValue, long.MinValue, true },
        new object[] { 0, 1, false },
        
        new object[] { new Struct {Field1 = 1, Field2 = 2}, new Struct {Field1 = 1, Field2 = 2}, true },
        new object[] { new Struct {Field1 = 1, Field2 = 2}, new Struct {Field1 = 1, Field2 = 1}, false},
        
        new object[] {new int[] {1, 2, 3}, new int[] {1, 2, 3}, true},
        new object[] {new int[] {1, 2, 3}, new int[] {0, 0, 0}, false},
        
        new object[] {new Struct[]
            {
                new() { Field1 = 1, Field2 = 2}, new()
            },

            new Struct[]
            {
                new() { Field1 = 1, Field2 = 2}, new()
            },
            true
        },
        
        new object[] {new Node(), new Node(), true},
        new object[] {nodeStub, nodeStub, true},
        new object[] {new Node {next = null}, new Node(), true},
        new object[] {new Node (), nodeStub, true},

        new object[] {new Node {value = 1}, new Node(), false},
        new object[] {new Node {value = 2}, new Node(), false},
        new object[] {new Node {value = 1, next = nodeStub}, new Node {value = 1, next = nodeStub}, true},
        new object[] {new Node {value = 1, next = nodeStub}, new Node {value = 1, next = new Node()}, true},
        new object[] {new Node {value = 1, next = nodeStub}, new Node {value = 1, next = null}, false},
        
        
        new object[] {null, null, true},
        new object[] {new Struct(), null, false},
        new object[] {null, new Node(), false}
    };
    
    private struct Struct
    {
        public int Field1;
        public long Field2;
    }
}