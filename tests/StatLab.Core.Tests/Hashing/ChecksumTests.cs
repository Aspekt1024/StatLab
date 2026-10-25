using StatLab.Core.Hashing;

namespace StatLab.Core.Tests.Hashing;

public class ChecksumTests
{
    [Fact]
    public void Empty_checksum_is_the_FNV_offset_basis()
    {
        Assert.Equal(0xCBF29CE484222325UL, new Checksum().Value);
    }

    // Published FNV-1a 64-bit test vectors, so the hash is the standard one and stays identical everywhere.
    [Theory]
    [InlineData("a", 0xAF63DC4C8601EC8CUL)]
    [InlineData("foobar", 0x85944171F73967E8UL)]
    public void Bytes_match_published_FNV_1a_vectors(string ascii, ulong expected)
    {
        Checksum checksum = new();
        foreach (var character in ascii)
        {
            checksum.AddByte((byte)character);
        }

        Assert.Equal(expected, checksum.Value);
    }

    [Fact]
    public void Same_fields_give_the_same_checksum()
    {
        Checksum first = new();
        Checksum second = new();

        first.Add(12.5);
        first.Add("vertical");
        second.Add(12.5);
        second.Add("vertical");

        Assert.Equal(first.Value, second.Value);
    }

    // The promise is identical bits, and 0.0 and -0.0 are different bits.
    [Fact]
    public void Zero_and_negative_zero_hash_differently()
    {
        Checksum positive = new();
        Checksum negative = new();

        positive.Add(0.0);
        negative.Add(-0.0);

        Assert.NotEqual(positive.Value, negative.Value);
    }

    [Fact]
    public void Where_one_string_ends_and_the_next_begins_matters()
    {
        Checksum first = new();
        Checksum second = new();

        first.Add("ab");
        first.Add("c");
        second.Add("a");
        second.Add("bc");

        Assert.NotEqual(first.Value, second.Value);
    }
}
