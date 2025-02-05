using System.Diagnostics;
using System.Reflection;

namespace TestExecutor.Core;


public static class ObjectsComparer
{
    private class ObjectComparer: EqualityComparer<object>
    {
        internal readonly HashSet<(object, object)> _compared = [];

        private bool StructurallyEquals(object expected, object actual)
        {
            var xtype = expected.GetType();
            if (xtype != actual.GetType()) return false;
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (var field in xtype.GetFields(flags))
            {
                if (!typeof(MulticastDelegate).IsAssignableFrom(field.FieldType) &&
                    !field.Name.Contains("threadid", StringComparison.OrdinalIgnoreCase) &&
                    !Equals(field.GetValue(expected), field.GetValue(actual))) 
                    return false;
            }
            
            return true;
        }

        private bool ContentwiseEqual(Array expected, Array actual)
        {
            Debug.Assert(expected.GetType() == actual.GetType());
            if (expected.Rank != actual.Rank) return false;
            for (var i = 0; i < expected.Rank; i++)
            {
                if (expected.GetLength(i) != actual.GetLength(i) || expected.GetLowerBound(i) != actual.GetLowerBound(i))
                    return false;
            }
            
            var expectedEnumerator = expected.GetEnumerator();
            var actualEnumerator = actual.GetEnumerator();
            while (expectedEnumerator.MoveNext() && actualEnumerator.MoveNext())
            {
                if (!Equals(expectedEnumerator.Current, actualEnumerator.Current))
                {
                    return false;
                }
            }
            return true;
        }
        

        
        public override bool Equals(object? expected, object? actual)
        {
            if (expected is null && actual is null) return true;
            if (expected is null || actual is null) return false;
            
            var type = expected.GetType();
            if (type != actual.GetType()) return false;
            
            if (ReferenceEquals(expected, actual)) return true;

            if (type.IsPrimitive || type == typeof(Pointer) || type == typeof(string) || type.IsEnum)
            {
                return expected.Equals(actual);
            }
            
            if (!_compared.Add((expected, actual)))
            {
                return true;
            }

            if (expected is Array expectedArray && actual is Array actualArray)
            {
                return ContentwiseEqual(expectedArray, actualArray);
            }
            
            return StructurallyEquals(expected, actual);
        }

        public override int GetHashCode(object obj)
        {
            throw new UnreachableException("Should not be called");
        }
    }
    
    public static bool Equals(object? expected, object? actual)
    {
        var comparer = new ObjectComparer();
        try
        {
            return comparer.Equals(expected, actual);
        }
        finally
        {
            comparer._compared.Clear();
        }
    }
    
}