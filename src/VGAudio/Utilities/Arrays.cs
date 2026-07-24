using System;

namespace VGAudio.Utilities;

public static class Arrays
{
    public static T[] Generate<T>(int count, Func<int, T> elementGenerator)
    {
        var table = new T[count];
        for (var i = 0; i < count; i++)
        {
            table[i] = elementGenerator(i);
        }
        return table;
    }

    public static void GetJaggedArrayInfo(Type type, out Type baseType, out int rank)
    {
        rank = -1;
        baseType = type;

        while (type != null)
        {
            baseType = type;
            type = type.GetElementType();
            rank++;
        }
    }

    public static Type MakeJaggedArrayType(Type elementType, int rank)
    {
        var type = elementType;
        for (var i = 0; i < rank; i++)
        {
            type = WrapArrayType(type);
        }
        return type;
    }

    private static Type WrapArrayType(Type type)
    {
        if (type == typeof(byte)) return typeof(byte[]);
        if (type == typeof(sbyte)) return typeof(sbyte[]);
        if (type == typeof(char)) return typeof(char[]);
        if (type == typeof(short)) return typeof(short[]);
        if (type == typeof(ushort)) return typeof(ushort[]);
        if (type == typeof(int)) return typeof(int[]);
        if (type == typeof(uint)) return typeof(uint[]);
        if (type == typeof(long)) return typeof(long[]);
        if (type == typeof(ulong)) return typeof(ulong[]);
        if (type == typeof(float)) return typeof(float[]);
        if (type == typeof(double)) return typeof(double[]);

        if (type == typeof(byte[])) return typeof(byte[][]);
        if (type == typeof(sbyte[])) return typeof(sbyte[][]);
        if (type == typeof(char[])) return typeof(char[][]);
        if (type == typeof(short[])) return typeof(short[][]);
        if (type == typeof(ushort[])) return typeof(ushort[][]);
        if (type == typeof(int[])) return typeof(int[][]);
        if (type == typeof(uint[])) return typeof(uint[][]);
        if (type == typeof(long[])) return typeof(long[][]);
        if (type == typeof(ulong[])) return typeof(ulong[][]);
        if (type == typeof(float[])) return typeof(float[][]);
        if (type == typeof(double[])) return typeof(double[][]);

        throw new NotSupportedException($"Unsupported jagged array element type '{type}'.");
    }
}