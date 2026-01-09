using System.Runtime.CompilerServices;

public unsafe static class GCInspector
{
    // Offset is consistent across all primitive array types (int[], float[], etc.)
    static readonly int ReliableOffset = GetOffset();

    static int GetOffset()
    {
        int[] dummy = new int[1];
        // Distance between where the managed 'ref' points and where the 'data' starts
        return (int)((byte*)Unsafe.AsPointer(ref dummy[0]) - (byte*)Unsafe.As<int[], IntPtr>(ref dummy));
    }

    /// <summary>
    /// Checks if a pointer belongs to a managed array of the specified type.
    /// </summary>
    public static bool IsManagedArray(void* dataPtr, Type elementType)
    {
        if (dataPtr == null || elementType == null) return false;

        // Reconstruct the array type (e.g., float -> float[])
        Type arrayType = elementType.MakeArrayType();
        
        byte* mtLocation = (byte*)dataPtr - ReliableOffset;
        IntPtr actualMT = *(IntPtr*)mtLocation;
        IntPtr expectedMT = arrayType.TypeHandle.Value;

        return actualMT == expectedMT;
    }

    /// <summary>
    /// Returns the managed array as an 'object' (which can be cast to int[], float[], etc.)
    /// </summary>
    public static object? TryGetManagedArray(void* dataPtr, Type elementType)
    {
        if (!IsManagedArray(dataPtr, elementType)) return null;

        void* objectRefPtr = (byte*)dataPtr - ReliableOffset;
        
        // Use Unsafe.AsRef<object> to get a managed reference to the reconstructed object
        return Unsafe.AsRef<object>(&objectRefPtr);
    }
}
