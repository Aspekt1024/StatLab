using StatLab.Core.Random;

namespace StatLab.Core.Tests.Random;

public class DeterministicRngTests
{
    private const double Tolerance = 1e-15;
    private const ulong Seed = 42;

    // Guards the cross-runtime promise: if the algorithm changes, every recorded result changes.
    // Expected values come from an independent Python port of the reference C code
    // (checked against splitmix64(0) = 0xe220a8397b1dcdaf and xoshiro256** {1,2,3,4} → 11520, 0).
    [Fact]
    public void Seed_42_produces_the_reference_sequence()
    {
        DeterministicRng rng = new(Seed);

        Assert.Equal(0.08386297105988216, rng.NextDouble(), Tolerance);
        Assert.Equal(0.3789802506626686, rng.NextDouble(), Tolerance);
        Assert.Equal(0.6800434110281394, rng.NextDouble(), Tolerance);
    }

    [Fact]
    public void Same_seed_produces_the_same_sequence()
    {
        DeterministicRng first = new(Seed);
        DeterministicRng second = new(Seed);

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(first.NextDouble(), second.NextDouble(), Tolerance);
        }
    }

    // Parallel runs are seeded baseSeed + runIndex, so neighbouring seeds must not repeat each other.
    [Fact]
    public void Neighbouring_seeds_produce_different_sequences()
    {
        DeterministicRng first = new(Seed);
        DeterministicRng second = new(Seed + 1);

        Assert.NotEqual(first.NextDouble(), second.NextDouble());
    }

    [Fact]
    public void NextDouble_is_at_least_zero_and_below_one()
    {
        DeterministicRng rng = new(Seed);

        for (var i = 0; i < 10_000; i++)
        {
            var value = rng.NextDouble();
            Assert.InRange(value, 0.0, Math.BitDecrement(1.0));
        }
    }

    [Fact]
    public void Clone_continues_the_same_sequence()
    {
        DeterministicRng original = new(Seed);
        original.NextDouble();

        DeterministicRng clone = original.Clone();

        Assert.Equal(original.NextDouble(), clone.NextDouble(), Tolerance);
    }

    [Fact]
    public void Drawing_from_a_clone_does_not_advance_the_original()
    {
        DeterministicRng original = new(Seed);
        DeterministicRng clone = original.Clone();

        clone.NextDouble();
        clone.NextDouble();

        // The original still produces the first value of the seed 42 sequence.
        Assert.Equal(0.08386297105988216, original.NextDouble(), Tolerance);
    }

    // Restoring a snapshot's rng must replay the same rolls, or rollback diverges.
    [Fact]
    public void CopyFrom_continues_the_source_sequence()
    {
        DeterministicRng source = new(Seed);
        source.NextDouble();
        DeterministicRng target = new(Seed + 1);

        target.CopyFrom(source);

        Assert.Equal(source.NextDouble(), target.NextDouble(), Tolerance);
    }
}
