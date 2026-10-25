using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using StatLab.Core.Items;
using StatLab.Core.Progression;
using StatLab.Core.Rulesets;
using StatLab.Core.Stats;

namespace StatLab.Core;

// Everything loaded from one ruleset's data folder, built once by StatLab.Data and never changed.
// Lists are sorted by id (ordinal), so iteration order is the same on every machine whatever order files loaded in:
// seeded picks from a list (e.g. loot) and sim output then stay reproducible.
public sealed class GameData
{
    private readonly Dictionary<ItemId, ItemDefinition> _items = new();
    private readonly Dictionary<ItemId, Modifier[]> _itemStats = new();
    private readonly Dictionary<ClassId, ClassDefinition> _classes = new();

    // Computes every item's stats now, so a bad table entry fails here rather than at the first equip.
    // Throws for repeated ids.
    public GameData(
        IRuleset ruleset,
        ItemBudgetTables budgetTables,
        IEnumerable<ItemDefinition> items,
        IEnumerable<ClassDefinition> classes)
    {
        Ruleset = ruleset;
        BudgetTables = budgetTables;

        foreach (ItemDefinition item in items)
        {
            if (!_items.TryAdd(item.Id, item))
            {
                throw new ArgumentException($"Item id '{item.Id.Value}' appears twice.", nameof(items));
            }

            _itemStats.Add(item.Id, ItemBudget.Calculate(item, budgetTables));
        }

        foreach (ClassDefinition definition in classes)
        {
            if (!_classes.TryAdd(definition.Id, definition))
            {
                throw new ArgumentException($"Class id '{definition.Id.Value}' appears twice.", nameof(classes));
            }
        }

        ItemDefinition[] sortedItems = _items.Values.ToArray();
        Array.Sort(sortedItems, (a, b) => string.CompareOrdinal(a.Id.Value, b.Id.Value));
        Items = Array.AsReadOnly(sortedItems);

        ClassDefinition[] sortedClasses = _classes.Values.ToArray();
        Array.Sort(sortedClasses, (a, b) => string.CompareOrdinal(a.Id.Value, b.Id.Value));
        Classes = Array.AsReadOnly(sortedClasses);
    }

    public IRuleset Ruleset { get; }

    public ItemBudgetTables BudgetTables { get; }

    // Sorted by id, ordinal.
    public ReadOnlyCollection<ItemDefinition> Items { get; }

    // Sorted by id, ordinal.
    public ReadOnlyCollection<ClassDefinition> Classes { get; }

    public ItemDefinition Item(ItemId id)
    {
        return TryGetItem(id, out ItemDefinition? item)
            ? item
            : throw new ArgumentException($"Unknown item '{id.Value}'.", nameof(id));
    }

    public bool TryGetItem(ItemId id, [NotNullWhen(true)] out ItemDefinition? item)
    {
        return _items.TryGetValue(id, out item);
    }

    // The item's stats, computed once from the budget tables: Flat modifiers with no source yet.
    public ReadOnlySpan<Modifier> ItemStats(ItemId id)
    {
        return _itemStats.TryGetValue(id, out Modifier[]? stats)
            ? stats
            : throw new ArgumentException($"Unknown item '{id.Value}'.", nameof(id));
    }

    public ClassDefinition Class(ClassId id)
    {
        return TryGetClass(id, out ClassDefinition? definition)
            ? definition
            : throw new ArgumentException($"Unknown class '{id.Value}'.", nameof(id));
    }

    public bool TryGetClass(ClassId id, [NotNullWhen(true)] out ClassDefinition? definition)
    {
        return _classes.TryGetValue(id, out definition);
    }
}
