namespace StatLab.Core.Random;

// Seeded random numbers for Core (AGENTS.md rule 3). Same seed, same sequence, on every runtime.
// xoshiro256** (Blackman & Vigna), with its state set from the seed by SplitMix64 so nearby seeds
// (baseSeed + runIndex) give unrelated sequences. Written out in full rather than using System.Random
// or BitOperations, so the output is identical on .NET, Unity and Godot.
public sealed class DeterministicRng
{
    private ulong _s0;
    private ulong _s1;
    private ulong _s2;
    private ulong _s3;

    public DeterministicRng(ulong seed)
    {
        var splitMix = seed;
        _s0 = SplitMix64(ref splitMix);
        _s1 = SplitMix64(ref splitMix);
        _s2 = SplitMix64(ref splitMix);
        _s3 = SplitMix64(ref splitMix);
    }

    private DeterministicRng(ulong s0, ulong s1, ulong s2, ulong s3)
    {
        _s0 = s0;
        _s1 = s1;
        _s2 = s2;
        _s3 = s3;
    }

    // Uniform in [0, 1): the top 53 bits of the next output, scaled by 2^-53.
    public double NextDouble()
    {
        return (NextULong() >> 11) * (1.0 / (1UL << 53));
    }

    // A copy that continues from the same point; drawing from one doesn't advance the other.
    public DeterministicRng Clone()
    {
        return new DeterministicRng(_s0, _s1, _s2, _s3);
    }

    private ulong NextULong()
    {
        var result = RotateLeft(_s1 * 5, 7) * 9;
        var t = _s1 << 17;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = RotateLeft(_s3, 45);

        return result;
    }

    private static ulong SplitMix64(ref ulong state)
    {
        state += 0x9E3779B97F4A7C15;
        var z = state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
        return z ^ (z >> 31);
    }

    private static ulong RotateLeft(ulong value, int count)
    {
        return (value << count) | (value >> (64 - count));
    }
}
