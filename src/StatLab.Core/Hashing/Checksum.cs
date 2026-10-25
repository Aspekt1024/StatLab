namespace StatLab.Core.Hashing;

// FNV-1a 64-bit, fed one field at a time. Written out in Core so the same state gives the same value on
// every machine and runtime (System.HashCode is seeded randomly per process). Not cryptographic.
// A class, not a struct, so passing it to AddToChecksum methods can't silently hash into a copy.
internal sealed class Checksum
{
    private const ulong OffsetBasis = 0xCBF29CE484222325;
    private const ulong Prime = 0x100000001B3;

    public ulong Value { get; private set; } = OffsetBasis;

    public void AddByte(byte value)
    {
        Value = (Value ^ value) * Prime;
    }

    // Little-endian bytes, written out so the result doesn't depend on the machine's byte order.
    public void Add(ulong value)
    {
        for (var shift = 0; shift < 64; shift += 8)
        {
            AddByte((byte)(value >> shift));
        }
    }

    public void Add(int value)
    {
        Add((ulong)value);
    }

    // Exact bits, so 0.0 and -0.0 differ: the promise is identical bits.
    public void Add(double value)
    {
        Add((ulong)BitConverter.DoubleToInt64Bits(value));
    }

    // Length first, so "ab" + "c" and "a" + "bc" differ.
    public void Add(string value)
    {
        Add(value.Length);
        foreach (var character in value)
        {
            Add((int)character);
        }
    }

    // A value's hash on its own, for combining items whose order doesn't matter (by adding them up).
    public static ulong Of(ulong value)
    {
        Checksum checksum = new();
        checksum.Add(value);
        return checksum.Value;
    }
}
