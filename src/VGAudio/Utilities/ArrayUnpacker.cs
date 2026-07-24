using System;
using System.IO;
using System.IO.Compression;

namespace VGAudio.Utilities;

public static class ArrayUnpacker
{
    private static readonly Type[] TypeLookup =
    [
        typeof(byte),
        typeof(sbyte),
        typeof(char),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double)
    ];

    public static Array[] UnpackArrays(byte[] packedArrays)
    {
        packedArrays = TryDecompress(packedArrays);

        using var stream = new MemoryStream(packedArrays);
        using var reader = new BinaryReader(stream);
        int compressed = reader.ReadByte();
        int version = reader.ReadByte();
        if (compressed != 0 || version != 0) throw new InvalidDataException();

        int count = reader.ReadUInt16();
        var arrays = new Array[count];

        for (var i = 0; i < count; i++)
        {
            var id = reader.ReadByte();
            var type = reader.ReadByte();
            var outType = TypeLookup[Helpers.GetHighNibble(type)];
            var rank = Helpers.GetLowNibble(type);
            arrays[id] = UnpackArray(reader, outType, rank);
        }

        return arrays;
    }

    private static Array UnpackArray(BinaryReader reader, Type outType, int rank)
    {
        var modeType = reader.ReadByte();
        if (modeType == byte.MaxValue) return null;

        var mode = Helpers.GetHighNibble(modeType);
        var storedType = TypeLookup[Helpers.GetLowNibble(modeType)];
        var elementType = Arrays.MakeJaggedArrayType(outType, rank - 1);

        switch (mode)
        {
            case 0:
                {
                    int length = reader.ReadUInt16();

                    if (rank == 1)
                    {
                        return ReadArray(reader, storedType, elementType, length);
                    }

                    var array = CreateArray(elementType, length);

                    for (var i = 0; i < length; i++)
                    {
                        array.SetValue(UnpackArray(reader, outType, rank - 1), i);
                    }

                    return array;
                }
            case 1:
                {
                    var dimensions = new int[rank];

                    for (var d = 0; d < dimensions.Length; d++)
                    {
                        dimensions[d] = reader.ReadUInt16();
                    }

                    return UnpackInternal(elementType, storedType, reader, 0, dimensions);
                }
            case 2:
                {
                    int length = reader.ReadUInt16();
                    var lengths = new int[length];

                    for (var i = 0; i < length; i++)
                    {
                        lengths[i] = reader.ReadUInt16();
                    }

                    var array = CreateArray(elementType, length);

                    for (var i = 0; i < length; i++)
                    {
                        array.SetValue(ReadArray(reader, storedType, outType, lengths[i]), i);
                    }

                    return array;
                }

            default:
                throw new InvalidDataException();
        }
    }

    private static Array ReadArray(BinaryReader reader, Type storedType, Type outType, int length)
    {
        if (length == ushort.MaxValue) return null;

        var lengthBytes = length * SizeOfPrimitive(storedType);
        var array = CreatePrimitiveArray(storedType, length);
        var bytes = reader.ReadBytes(lengthBytes);
        Buffer.BlockCopy(bytes, 0, array, 0, lengthBytes);

        return storedType == outType ? array : CastArray(array, outType);
    }

    private static Array CastArray(Array inArray, Type outType)
    {
        var outArray = CreatePrimitiveArray(outType, inArray.Length);

        for (var i = 0; i < inArray.Length; i++)
        {
            var inValue = inArray.GetValue(i);
            var outValue = Convert.ChangeType(inValue, outType);
            outArray.SetValue(outValue, i);
        }
        return outArray;
    }

    private static byte[] TryDecompress(byte[] data)
    {
        var compressed = data[0] == 1;
        if (compressed)
        {
            var decompressedLength = BitConverter.ToInt32(data, 1);
            data = Inflate(data, 5, decompressedLength);
        }
        return data;
    }

    private static byte[] Inflate(byte[] compressed, int startIndex, int length)
    {
        var inflatedBytes = new byte[length];
        using var stream = new MemoryStream(compressed);
        stream.Position = startIndex;
        using var deflate = new DeflateStream(stream, CompressionMode.Decompress);
        deflate.ReadExactly(inflatedBytes);
        return inflatedBytes;
    }

    private static Array UnpackInternal(Type outType, Type storedType, BinaryReader reader, int depth, int[] dimensions)
    {
        if (depth >= dimensions.Length) return null;
        if (depth == dimensions.Length - 1)
        {
            return ReadArray(reader, storedType, outType, dimensions[depth]);
        }

        var array = CreateArray(outType, dimensions[depth]);
        var elementType = GetArrayElementType(outType);

        for (var i = 0; i < dimensions[depth]; i++)
        {
            array.SetValue(UnpackInternal(elementType, storedType, reader, depth + 1, dimensions), i);
        }

        return array;
    }

    private static int SizeOfPrimitive(Type type)
    {
        if (type == typeof(byte) || type == typeof(sbyte)) return sizeof(byte);
        if (type == typeof(char) || type == typeof(short) || type == typeof(ushort)) return sizeof(short);
        if (type == typeof(int) || type == typeof(uint) || type == typeof(float)) return sizeof(int);
        if (type == typeof(long) || type == typeof(ulong) || type == typeof(double)) return sizeof(long);
        throw new NotSupportedException($"Unsupported primitive type '{type}'.");
    }

    private static Array CreatePrimitiveArray(Type type, int length)
    {
        if (type == typeof(byte)) return new byte[length];
        if (type == typeof(sbyte)) return new sbyte[length];
        if (type == typeof(char)) return new char[length];
        if (type == typeof(short)) return new short[length];
        if (type == typeof(ushort)) return new ushort[length];
        if (type == typeof(int)) return new int[length];
        if (type == typeof(uint)) return new uint[length];
        if (type == typeof(long)) return new long[length];
        if (type == typeof(ulong)) return new ulong[length];
        if (type == typeof(float)) return new float[length];
        if (type == typeof(double)) return new double[length];
        throw new NotSupportedException($"Unsupported primitive type '{type}'.");
    }

    private static Array CreateArray(Type type, int length)
    {
        if (IsPrimitive(type)) return CreatePrimitiveArray(type, length);

        if (type == typeof(byte[])) return new byte[length][];
        if (type == typeof(sbyte[])) return new sbyte[length][];
        if (type == typeof(char[])) return new char[length][];
        if (type == typeof(short[])) return new short[length][];
        if (type == typeof(ushort[])) return new ushort[length][];
        if (type == typeof(int[])) return new int[length][];
        if (type == typeof(uint[])) return new uint[length][];
        if (type == typeof(long[])) return new long[length][];
        if (type == typeof(ulong[])) return new ulong[length][];
        if (type == typeof(float[])) return new float[length][];
        if (type == typeof(double[])) return new double[length][];

        if (type == typeof(byte[][])) return new byte[length][][];
        if (type == typeof(sbyte[][])) return new sbyte[length][][];
        if (type == typeof(char[][])) return new char[length][][];
        if (type == typeof(short[][])) return new short[length][][];
        if (type == typeof(ushort[][])) return new ushort[length][][];
        if (type == typeof(int[][])) return new int[length][][];
        if (type == typeof(uint[][])) return new uint[length][][];
        if (type == typeof(long[][])) return new long[length][][];
        if (type == typeof(ulong[][])) return new ulong[length][][];
        if (type == typeof(float[][])) return new float[length][][];
        if (type == typeof(double[][])) return new double[length][][];

        throw new NotSupportedException($"Unsupported array type '{type}'.");
    }

    private static Type GetArrayElementType(Type arrayType)
    {
        if (arrayType == typeof(byte[])) return typeof(byte);
        if (arrayType == typeof(sbyte[])) return typeof(sbyte);
        if (arrayType == typeof(char[])) return typeof(char);
        if (arrayType == typeof(short[])) return typeof(short);
        if (arrayType == typeof(ushort[])) return typeof(ushort);
        if (arrayType == typeof(int[])) return typeof(int);
        if (arrayType == typeof(uint[])) return typeof(uint);
        if (arrayType == typeof(long[])) return typeof(long);
        if (arrayType == typeof(ulong[])) return typeof(ulong);
        if (arrayType == typeof(float[])) return typeof(float);
        if (arrayType == typeof(double[])) return typeof(double);

        if (arrayType == typeof(byte[][])) return typeof(byte[]);
        if (arrayType == typeof(sbyte[][])) return typeof(sbyte[]);
        if (arrayType == typeof(char[][])) return typeof(char[]);
        if (arrayType == typeof(short[][])) return typeof(short[]);
        if (arrayType == typeof(ushort[][])) return typeof(ushort[]);
        if (arrayType == typeof(int[][])) return typeof(int[]);
        if (arrayType == typeof(uint[][])) return typeof(uint[]);
        if (arrayType == typeof(long[][])) return typeof(long[]);
        if (arrayType == typeof(ulong[][])) return typeof(ulong[]);
        if (arrayType == typeof(float[][])) return typeof(float[]);
        if (arrayType == typeof(double[][])) return typeof(double[]);

        throw new NotSupportedException($"Unsupported array type '{arrayType}'.");
    }

    private static bool IsPrimitive(Type type) =>
        type == typeof(byte) || type == typeof(sbyte) || type == typeof(char) ||
        type == typeof(short) || type == typeof(ushort) ||
        type == typeof(int) || type == typeof(uint) ||
        type == typeof(long) || type == typeof(ulong) ||
        type == typeof(float) || type == typeof(double);
}
