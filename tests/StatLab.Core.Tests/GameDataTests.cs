using StatLab.Core.Items;
using StatLab.Core.Progression;
using StatLab.Core.Stats;
using StatLab.Core.Tests.Stats;

namespace StatLab.Core.Tests;

public class GameDataTests
{
    private static readonly SlotId Chest = new("chest");

    private static readonly ItemBudgetTables Tables = new(
        new Dictionary<int, double> { [20] = 40 },
        new Dictionary<SlotId, double> { [Chest] = 1.0 },
        new Dictionary<StatId, double> { [StatId.Strength] = 1.0, [StatId.Stamina] = 0.67 });

    private static ItemDefinition Item(string id, int itemLevel = 20)
    {
        return new ItemDefinition(new ItemId(id), Chest, itemLevel, 1.0,
            [new StatShare(StatId.Strength, 0.6), new StatShare(StatId.Stamina, 0.4)]);
    }

    private static ClassDefinition Class(string id)
    {
        return new ClassDefinition(new ClassId(id), new Dictionary<int, StatBlock> { [1] = new() });
    }

    private static GameData Build(ItemDefinition[] items, ClassDefinition[]? classes = null)
    {
        return new GameData(TestRuleset.Instance, Tables, items, classes ?? []);
    }

    // Ordinal: by character code, so "Axe" (A = 65) comes before "amulet" (a = 97). A culture-aware sort
    // would put "amulet" first, and could differ between machines with different language settings.
    [Fact]
    public void Items_are_sorted_by_id_ordinally_whatever_order_they_were_given()
    {
        GameData data = Build([Item("ring_b"), Item("amulet"), Item("Axe")]);

        var ids = data.Items.Select(item => item.Id.Value).ToArray();
        Assert.Equal(["Axe", "amulet", "ring_b"], ids);
    }

    [Fact]
    public void Classes_are_sorted_by_id()
    {
        GameData data = Build([], [Class("warrior"), Class("mage")]);

        var ids = data.Classes.Select(definition => definition.Id.Value).ToArray();
        Assert.Equal(["mage", "warrior"], ids);
    }

    [Fact]
    public void Item_returns_its_definition()
    {
        ItemDefinition chestplate = Item("chestplate");
        GameData data = Build([chestplate]);

        Assert.Same(chestplate, data.Item(new ItemId("chestplate")));
    }

    [Fact]
    public void Unknown_item_throws_naming_the_id()
    {
        GameData data = Build([Item("chestplate")]);

        ArgumentException error = Assert.Throws<ArgumentException>(() => data.Item(new ItemId("ring_of_mihgt")));
        Assert.Contains("ring_of_mihgt", error.Message);
    }

    [Fact]
    public void TryGetItem_returns_false_for_an_unknown_id()
    {
        GameData data = Build([Item("chestplate")]);

        Assert.False(data.TryGetItem(new ItemId("cloak"), out _));
    }

    [Fact]
    public void Unknown_class_throws_naming_the_id()
    {
        GameData data = Build([], [Class("warrior")]);

        ArgumentException error = Assert.Throws<ArgumentException>(() => data.Class(new ClassId("rogue")));
        Assert.Contains("rogue", error.Message);
    }

    [Fact]
    public void Item_stats_are_the_budget_formula_result()
    {
        GameData data = Build([Item("chestplate")]);

        Modifier[] expected = ItemBudget.Calculate(Item("chestplate"), Tables);
        Assert.Equal(expected, data.ItemStats(new ItemId("chestplate")).ToArray());
    }

    // Stats are computed at build, so a missing table entry fails here rather than at the first equip.
    [Fact]
    public void Item_whose_stats_cannot_be_calculated_fails_at_build()
    {
        Assert.Throws<ArgumentException>(() => Build([Item("chestplate", itemLevel: 7)]));
    }

    [Fact]
    public void Repeated_item_id_throws()
    {
        Assert.Throws<ArgumentException>(() => Build([Item("chestplate"), Item("chestplate")]));
    }

    [Fact]
    public void Repeated_class_id_throws()
    {
        Assert.Throws<ArgumentException>(() => Build([], [Class("warrior"), Class("warrior")]));
    }
}
