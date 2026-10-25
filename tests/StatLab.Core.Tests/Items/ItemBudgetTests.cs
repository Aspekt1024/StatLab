using StatLab.Core.Items;
using StatLab.Core.Stats;

namespace StatLab.Core.Tests.Items;

public class ItemBudgetTests
{
    private const double Tolerance = 1e-9;
    private static readonly SlotId Chest = new("chest");
    private static readonly SlotId Ring = new("ring");

    // The worked example's numbers (docs/reference/item-budget.md): level 20 base budget 40, chest × 1.0,
    // Strength weight 1.0, Stamina weight 0.67. Level 5 and the ring slot are for the extra cases.
    private static readonly ItemBudgetTables Tables = new(
        new Dictionary<int, double> { [5] = 5, [20] = 40 },
        new Dictionary<SlotId, double> { [Chest] = 1.0, [Ring] = 0.5 },
        new Dictionary<StatId, double> { [StatId.Strength] = 1.0, [StatId.Stamina] = 0.67 });

    private static ItemDefinition Item(
        int itemLevel = 20,
        SlotId? slot = null,
        double powerModifier = 1.0,
        double strengthShare = 0.6,
        double staminaShare = 0.4)
    {
        return new ItemDefinition(
            new ItemId("test_item"),
            slot ?? Chest,
            itemLevel,
            powerModifier,
            [new StatShare(StatId.Strength, strengthShare), new StatShare(StatId.Stamina, staminaShare)]);
    }

    [Fact]
    public void Worked_example_from_the_spec()
    {
        Modifier[] stats = ItemBudget.Calculate(Item(), Tables);

        // Budget 40 × 1.0 × 1.0 = 40. Strength 0.6 × 40 ÷ 1.0 = 24; Stamina 0.4 × 40 ÷ 0.67 ≈ 23.88 → 24
        Assert.Equal(24, stats[0].Value, Tolerance);
        Assert.Equal(24, stats[1].Value, Tolerance);
    }

    [Fact]
    public void Stats_are_flat_modifiers_in_share_order_with_no_source_yet()
    {
        Modifier[] stats = ItemBudget.Calculate(Item(), Tables);

        Modifier[] expected =
        [
            new(StatId.Strength, ModifierType.Flat, 24, default),
            new(StatId.Stamina, ModifierType.Flat, 24, default),
        ];
        Assert.Equal(expected, stats);
    }

    [Fact]
    public void Slot_multiplier_scales_the_budget()
    {
        Modifier[] stats = ItemBudget.Calculate(Item(slot: Ring), Tables);

        // Budget 40 × 0.5 = 20. Strength 0.6 × 20 = 12; Stamina 0.4 × 20 ÷ 0.67 ≈ 11.94 → 12
        Assert.Equal(12, stats[0].Value, Tolerance);
        Assert.Equal(12, stats[1].Value, Tolerance);
    }

    [Fact]
    public void Power_modifier_scales_the_budget()
    {
        Modifier[] stats = ItemBudget.Calculate(Item(powerModifier: 1.5), Tables);

        // Budget 40 × 1.5 = 60. Strength 0.6 × 60 = 36; Stamina 0.4 × 60 ÷ 0.67 ≈ 35.82 → 36
        Assert.Equal(36, stats[0].Value, Tolerance);
        Assert.Equal(36, stats[1].Value, Tolerance);
    }

    [Fact]
    public void Halves_round_away_from_zero()
    {
        Modifier[] stats = ItemBudget.Calculate(Item(itemLevel: 5, strengthShare: 0.5, staminaShare: 0.5), Tables);

        // Budget 5. Strength 0.5 × 5 ÷ 1.0 = 2.5 → 3 (banker's rounding would give 2)
        Assert.Equal(3, stats[0].Value, Tolerance);
    }

    [Fact]
    public void Missing_item_level_throws()
    {
        Assert.Throws<ArgumentException>(() => ItemBudget.Calculate(Item(itemLevel: 7), Tables));
    }

    [Fact]
    public void Unknown_slot_throws()
    {
        Assert.Throws<ArgumentException>(() => ItemBudget.Calculate(Item(slot: new SlotId("cloak")), Tables));
    }

    [Fact]
    public void Stat_with_no_weight_throws()
    {
        ItemDefinition item = new(
            new ItemId("test_item"), Chest, 20, 1.0, [new StatShare(StatId.Agility, 1.0)]);

        Assert.Throws<ArgumentException>(() => ItemBudget.Calculate(item, Tables));
    }

    // The tables copy their inputs, so changing a dictionary afterwards can't change item stats.
    [Fact]
    public void Changing_the_source_tables_after_creation_changes_nothing()
    {
        Dictionary<int, double> levels = new() { [20] = 40 };
        ItemBudgetTables tables = new(
            levels,
            new Dictionary<SlotId, double> { [Chest] = 1.0 },
            new Dictionary<StatId, double> { [StatId.Strength] = 1.0, [StatId.Stamina] = 0.67 });

        levels[20] = 1000;

        Assert.Equal(40, tables.BaseBudget(20), Tolerance);
    }
}
