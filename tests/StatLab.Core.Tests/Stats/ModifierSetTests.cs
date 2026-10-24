using StatLab.Core.Stats;

namespace StatLab.Core.Tests.Stats;

public class ModifierSetTests
{
    private const double Tolerance = 1e-9;
    private static readonly SourceId Sword = new(1);
    private static readonly SourceId Ring = new(2);

    [Fact]
    public void New_set_has_no_modifiers_for_any_stat()
    {
        ModifierSet set = new();

        foreach (StatId stat in Enum.GetValues<StatId>())
        {
            Assert.True(set.For(stat).IsEmpty);
        }
    }

    [Fact]
    public void Added_modifiers_are_grouped_by_stat()
    {
        Modifier strength = new(StatId.Strength, ModifierType.Flat, 5, Sword);
        Modifier armor = new(StatId.Armor, ModifierType.Flat, 10, Sword);
        ModifierSet set = new();

        set.Add(Sword, [strength, armor]);

        Assert.Equal([strength], set.For(StatId.Strength).ToArray());
        Assert.Equal([armor], set.For(StatId.Armor).ToArray());
        Assert.True(set.For(StatId.Agility).IsEmpty);
    }

    [Fact]
    public void Removing_by_source_restores_the_original_value()
    {
        ModifierSet set = new();
        set.Add(Sword, [new Modifier(StatId.Strength, ModifierType.Flat, 5, Sword)]);

        set.Remove(Sword);
        var result = StatPipeline.Calculate(10, set.For(StatId.Strength));

        // 10 with no modifiers left = 10
        Assert.Equal(10, result, Tolerance);
    }

    [Fact]
    public void Removing_a_source_removes_its_modifiers_from_every_stat()
    {
        ModifierSet set = new();
        set.Add(Sword, [
            new Modifier(StatId.Strength, ModifierType.Flat, 5, Sword),
            new Modifier(StatId.CritChance, ModifierType.Flat, 0.01, Sword),
        ]);

        set.Remove(Sword);

        Assert.True(set.For(StatId.Strength).IsEmpty);
        Assert.True(set.For(StatId.CritChance).IsEmpty);
    }

    // Two copies of the same item are separate sources.
    [Fact]
    public void Removing_one_source_leaves_other_sources()
    {
        Modifier fromRing = new(StatId.Strength, ModifierType.Flat, 3, Ring);
        ModifierSet set = new();
        set.Add(Sword, [new Modifier(StatId.Strength, ModifierType.Flat, 5, Sword)]);
        set.Add(Ring, [fromRing]);

        set.Remove(Sword);

        Assert.Equal([fromRing], set.For(StatId.Strength).ToArray());
    }

    [Fact]
    public void Adding_the_same_source_twice_throws()
    {
        ModifierSet set = new();
        set.Add(Sword, [new Modifier(StatId.Strength, ModifierType.Flat, 5, Sword)]);

        Assert.Throws<ArgumentException>(() => set.Add(Sword, []));
    }

    [Fact]
    public void Adding_a_modifier_from_a_different_source_throws()
    {
        ModifierSet set = new();
        Modifier fromRing = new(StatId.Strength, ModifierType.Flat, 3, Ring);

        Assert.Throws<ArgumentException>(() => set.Add(Sword, [fromRing]));
    }

    [Fact]
    public void Removing_an_unknown_source_returns_false()
    {
        ModifierSet set = new();

        Assert.False(set.Remove(Sword));
    }

    // Floating-point sums depend on order, so the order must not depend on how sources were added.
    [Fact]
    public void Modifiers_are_ordered_by_type_then_value_regardless_of_add_order()
    {
        Modifier percent = new(StatId.Strength, ModifierType.PercentAdd, 0.1, Sword);
        Modifier highFlat = new(StatId.Strength, ModifierType.Flat, 7, Sword);
        Modifier lowFlat = new(StatId.Strength, ModifierType.Flat, 2, Ring);
        ModifierSet set = new();

        set.Add(Sword, [percent, highFlat]);
        set.Add(Ring, [lowFlat]);

        Assert.Equal([lowFlat, highFlat, percent], set.For(StatId.Strength).ToArray());
    }

    [Fact]
    public void Clone_has_the_same_modifiers()
    {
        Modifier strength = new(StatId.Strength, ModifierType.Flat, 5, Sword);
        ModifierSet original = new();
        original.Add(Sword, [strength]);

        ModifierSet clone = original.Clone();

        Assert.Equal([strength], clone.For(StatId.Strength).ToArray());
    }

    [Fact]
    public void Adding_to_a_clone_does_not_change_the_original()
    {
        ModifierSet original = new();
        ModifierSet clone = original.Clone();

        clone.Add(Ring, [new Modifier(StatId.Strength, ModifierType.Flat, 3, Ring)]);

        Assert.True(original.For(StatId.Strength).IsEmpty);
    }

    [Fact]
    public void Removing_from_a_clone_does_not_change_the_original()
    {
        Modifier strength = new(StatId.Strength, ModifierType.Flat, 5, Sword);
        ModifierSet original = new();
        original.Add(Sword, [strength]);
        ModifierSet clone = original.Clone();

        clone.Remove(Sword);

        Assert.Equal([strength], original.For(StatId.Strength).ToArray());
    }

    [Fact]
    public void CopyFrom_replaces_existing_sources()
    {
        Modifier fromRing = new(StatId.Strength, ModifierType.Flat, 3, Ring);
        ModifierSet source = new();
        source.Add(Ring, [fromRing]);
        ModifierSet target = new();
        target.Add(Sword, [new Modifier(StatId.Strength, ModifierType.Flat, 5, Sword)]);

        target.CopyFrom(source);

        Assert.Equal([fromRing], target.For(StatId.Strength).ToArray());
        Assert.False(target.Remove(Sword));
    }
}
