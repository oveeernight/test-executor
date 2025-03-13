using System.Diagnostics;
using TestExecutor.CSharpUtils;
using TestExecutor.Utils;

namespace TestExecutor.Core;

public static class UnsafeUtils
{
    public static byte[] ObjToBytes(object obj)
    {
        switch (obj)
        {
            case null:
                var size = TypeUtils.internalSizeOf(typeof(object));
                return new byte[size];
            case byte b:
                return [b];
            case sbyte sb:
                var casted = unchecked((byte)sb);
                return [casted];
            case short s:
                return BitConverter.GetBytes(s);
            case ushort us:
                return BitConverter.GetBytes(us);
            case int i:
                return BitConverter.GetBytes(i);
            case uint ui:
                return BitConverter.GetBytes(ui);
            case long l:
                return BitConverter.GetBytes(l);
            case ulong ul:
                return BitConverter.GetBytes(ul);
            case float f:
                return BitConverter.GetBytes(f);
            case double d:
                return BitConverter.GetBytes(d);
            case char ch:
                return BitConverter.GetBytes(ch);
            case ValueType vt:
                return StructToBytes(vt);
            default:
                if (obj is Enum e)
                {
                    var o = Convert.ChangeType(e, TypeUtils.EnumUtils.getEnumUnderlyingTypeChecked(e.GetType()));
                    return ObjToBytes(o);
                }
                throw new InvalidOperationException($"Getting bytes from unexpected object {obj}");
        }
    }

    
    public static byte[] StructToBytes(ValueType s)
    {
        var t = s.GetType();
        var size = TypeUtils.internalSizeOf(t);
        var array = new byte[size];
        if (t.IsGenericType)
        {
            var fields = t.GetFields();
            foreach (var field in fields)
            {
                var value = field.GetValue(t);
                var fieldBytes = ObjToBytes(value);
                var offset = LayoutUtils.GetFieldOffset(field);
                Array.Copy(fieldBytes, 0, array, offset, fieldBytes.Length);
            }
        }
        else
        {
            var ptr = IntPtr.Zero;
            try
            {
                ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
                System.Runtime.InteropServices.Marshal.StructureToPtr(s, ptr, true);
                System.Runtime.InteropServices.Marshal.Copy(ptr, array, 0, size);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);
            }
        }
        return array;
    }

    public static object BytesToObject(byte[] bytes, Type t)
    {
        var span = new ReadOnlySpan<byte>(bytes);
        if (t == typeof(byte)) return span[0];
        if (t == typeof(sbyte)) return unchecked((sbyte)span[0]);
        if (t == typeof(short)) return BitConverter.ToInt16(span);
        if (t == typeof(ushort)) return BitConverter.ToUInt16(span);
        if (t == typeof(int)) return BitConverter.ToInt32(span);
        if (t == typeof(uint)) return BitConverter.ToUInt32(span);
        if (t == typeof(long)) return BitConverter.ToInt64(span);
        if (t == typeof(ulong)) return BitConverter.ToUInt64(span);
        if (t == typeof(float)) return BitConverter.ToSingle(span);
        if (t == typeof(double)) return BitConverter.ToDouble(span);
        if (t == typeof(char)) return BitConverter.ToChar(span);
        if (t == typeof(bool)) return BitConverter.ToBoolean(span);
        if (t == typeof(IntPtr))
        {
            return IntPtr.Size switch
            {
                sizeof(int) => IntPtr.CreateChecked(BitConverter.ToInt32(span)),
                sizeof(long) => IntPtr.CreateChecked(BitConverter.ToInt64(span)),
                _ => throw new InvalidOperationException($"Unexpected IntPtr size {IntPtr.Size}")
            };
        }

        if (t == typeof(UIntPtr))
        {
            return UIntPtr.Size switch
            {
                sizeof(uint) => UIntPtr.CreateChecked(BitConverter.ToUInt32(span)),
                sizeof(ulong) => UIntPtr.CreateChecked(BitConverter.ToUInt64(span)),
                _ => throw new InvalidOperationException($"Unexpected UIntPtr size {UIntPtr.Size}")
            };
        }

        if (t == typeof(System.Reflection.Pointer))
        {
            unsafe
            {
                IntPtr ptr = IntPtr.Size switch
                {
                    sizeof(int) => IntPtr.CreateChecked(BitConverter.ToInt32(span)),
                    sizeof(long) => IntPtr.CreateChecked(BitConverter.ToInt64(span)),
                    _ => throw new InvalidOperationException($"Unexpected IntPtr size {IntPtr.Size}")
                };
                return System.Reflection.Pointer.Box(ptr.ToPointer(), typeof(void).MakePointerType());
            }
        }

        if (t.IsEnum)
        {
            return BytesToObject(bytes, TypeUtils.EnumUtils.getEnumUnderlyingTypeChecked(t));
        }

        if (t.IsValueType)
        {
            return BytesToStruct(bytes, t);
        }

        if (!t.IsValueType && bytes.All(b => b == 0)) return null;
        throw new InvalidOperationException($"BytesToObject: unexpected type {t} and bytes are not zeros");
    }

    public static object BytesToNullable(byte[] bytes, Type t)
    {
        var fields = t.GetFields();
        if (fields.Length != 2)
        {
            throw new InvalidOperationException($"BytesToNullable: unexpected field count {fields.Length}");
        }
        var hasValueField = fields.First(f => f.Name == "hasValue");
        var hasValueOffset = LayoutUtils.GetFieldOffset(hasValueField);
        var hasValueSize = TypeUtils.internalSizeOf(t);
        var hasValueBytes = bytes[hasValueOffset..(hasValueOffset + hasValueSize - 1)];
        var hasValue = (bool)BytesToObject(hasValueBytes, typeof(bool));
        if (hasValue)
        {
            var valueField = fields.First(f => f.Name == "value");
            var valueFieldOffset = LayoutUtils.GetFieldOffset(valueField);
            var underlyingType = Nullable.GetUnderlyingType(t);
            var valueFieldSize = TypeUtils.internalSizeOf(underlyingType);
            var valueBytes = bytes[valueFieldOffset..(valueFieldOffset + valueFieldSize - 1)];
            return BytesToObject(valueBytes, underlyingType);
        }
        return 0;
    }
    
    public static object BytesToStruct(byte[] bytes, Type t)
    {
        if (t.IsGenericType)
        {
            if (TypeUtils.isNullable(t))
            {
                return BytesToNullable(bytes, t);
            }
            var obj = Reflection.defaultOf(t);
            var fields = t.GetFields();
            foreach (var fi in fields)
            {
                var offset = LayoutUtils.GetFieldOffset(fi);
                var fieldType = fi.FieldType;
                var fieldSize = TypeUtils.internalSizeOf(fieldType);
                var fieldBytes = bytes[offset..(offset + fieldSize - 1)];
                var value = BytesToObject(fieldBytes, fieldType);
                fi.SetValue(obj, value);
            }
            return obj;
        }
        Debug.Assert(t.GetFields().All(f => f.GetType().IsValueType));
        IntPtr ptr = IntPtr.Zero;
        var size = TypeUtils.internalSizeOf(t);
        try
        {
            ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, ptr, size);
            return System.Runtime.InteropServices.Marshal.PtrToStructure(ptr, t);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);
        }
    }
    
}