using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace VGAudio.Utilities;

public static class Helpers
{
    private static readonly sbyte[] SignedNibbles = [0, 1, 2, 3, 4, 5, 6, 7, -8, -7, -6, -5, -4, -3, -2, -1];

    private static readonly int[] MultiplyDeBruijnBitPosition =
    [
        0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
        8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(int value, int min, int max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Clamp(double value, double min, double max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short Clamp16(int value)
    {
        if (value > short.MaxValue)
            return short.MaxValue;
        if (value < short.MinValue)
            return short.MinValue;
        return (short)value;
    }

    public static sbyte Clamp4(int value)
    {
        if (value > 7)
            return 7;
        if (value < -8)
            return -8;
        return (sbyte)value;
    }

    public static byte GetHighNibble(byte value)
    {
        return (byte)(value >> 4 & 0xF);
    }

    public static byte GetLowNibble(byte value)
    {
        return (byte)(value & 0xF);
    }

    public static sbyte GetHighNibbleSigned(byte value)
    {
        return SignedNibbles[value >> 4 & 0xF];
    }

    public static sbyte GetLowNibbleSigned(byte value)
    {
        return SignedNibbles[value & 0xF];
    }

    public static byte CombineNibbles(int high, int low)
    {
        return (byte)(high << 4 | low & 0xF);
    }

    public static int BitCount(int v)
    {
        return BitCount(unchecked((uint)v));
    }

    public static int BitCount(uint v)
    {
        unchecked
        {
            v = v - (v >> 1 & 0x55555555);
            v = (v & 0x33333333) + (v >> 2 & 0x33333333);
            return (int)((v + (v >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;
        }
    }

    public static int GetNextMultiple(int value, int multiple)
    {
        if (multiple <= 0)
            return value;

        if (value % multiple == 0)
            return value;

        return value + multiple - value % multiple;
    }

    public static bool LoopPointsAreAligned(int loopStart, int alignmentMultiple)
    {
        return !(alignmentMultiple != 0 && loopStart % alignmentMultiple != 0);
    }

    public static BinaryReader GetBinaryReader(Stream stream, Endianness endianness)
    {
        return endianness == Endianness.LittleEndian
            ? new BinaryReader(stream, Encoding.UTF8, true)
            : new BinaryReaderBe(stream, Encoding.UTF8, true);
    }

    public static BinaryWriter GetBinaryWriter(Stream stream, Endianness endianness)
    {
        return endianness == Endianness.LittleEndian
            ? new BinaryWriter(stream, Encoding.UTF8, true)
            : new BinaryWriterBe(stream, Encoding.UTF8, true);
    }

    public static T CreateJaggedArray<T>(params int[] lengths)
    {
        if (lengths is not { Length: 2 })
        {
            throw new ArgumentException("Only 2D jagged arrays are supported.", nameof(lengths));
        }

        if (typeof(T) == typeof(double[][]))
        {
            return (T)(object)CreateJaggedArray2D<double>(lengths[0], lengths[1]);
        }

        if (typeof(T) == typeof(int[][]))
        {
            return (T)(object)CreateJaggedArray2D<int>(lengths[0], lengths[1]);
        }

        if (typeof(T) == typeof(short[][]))
        {
            return (T)(object)CreateJaggedArray2D<short>(lengths[0], lengths[1]);
        }

        if (typeof(T) == typeof(byte[][]))
        {
            return (T)(object)CreateJaggedArray2D<byte>(lengths[0], lengths[1]);
        }

        throw new NotSupportedException($"CreateJaggedArray<{typeof(T)}> is not supported.");
    }

    private static T[][] CreateJaggedArray2D<T>(int length0, int length1)
    {
        var array = new T[length0][];
        for (var i = 0; i < length0; i++)
        {
            array[i] = new T[length1];
        }

        return array;
    }

    public static int[] GetPrimes(int maxPrime)
    {
        var max = maxPrime / 2;
        var sieve = new byte[max];

        for (var i = 3; i * i < maxPrime; i += 2)
        {
            if (sieve[i >> 1] != 0) continue;
            for (var j = i * i; j < maxPrime; j += i * 2)
            {
                sieve[j >> 1] = 1;
            }
        }

        var primes = new List<int> { 2 };
        for (var i = 1; i < max; i++)
        {
            if (sieve[i] == 0)
            {
                primes.Add(i * 2 + 1);
            }
        }

        return [.. primes];
    }

    /// <summary>
    ///     Returns the floor of the base 2 logarithm of a specified number.
    /// </summary>
    /// <param name="value">The number whose logarithm is to be found.</param>
    /// <returns>The floor of the base 2 logarithm of <paramref name="value" />.</returns>
    public static int Log2(int value)
    {
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;

        return MultiplyDeBruijnBitPosition[(uint)(value * 0x07C4ACDDU) >> 27];
    }
}