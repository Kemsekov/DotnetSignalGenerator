using System.Runtime.InteropServices;
using SignalCore;

public static class PrimitivesExtensions
{
    public static byte[] ToBinaryArray(this IEnumerable<ushort> source, int ushortCount)
    {
        // 1. Pre-allocate the exact size: 2 bytes per ushort
        byte[] result = new byte[ushortCount * 2];
        
        // 2. Cast the byte array to a ushort span to write directly into it
        Span<ushort> destination = MemoryMarshal.Cast<byte, ushort>(result);
        
        int index = 0;
        foreach (var value in source)
        {
            if (index >= ushortCount) break; // Safety check
            destination[index++] = value;
        }
        return result;
    }
    public static ushort[] ToShortArray(this byte[] array)
    {
        // MemoryMarshal.Cast allows us to treat a byte Span as a short Span without copying
        // Then we call ToArray() to create the new short[] object
        return MemoryMarshal.Cast<byte, ushort>(array).ToArray();
    }
}