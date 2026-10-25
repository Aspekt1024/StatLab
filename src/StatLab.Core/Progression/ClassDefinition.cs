using StatLab.Core.Stats;

namespace StatLab.Core.Progression;

// A class's base stats at each level, one full row per level: any stat, primary or derived (e.g. a base
// MaxHealth that adds to Stamina's conversion). Read only through BaseStats, so the table could later be
// generated from a curve without changing callers. Copies its inputs and hands out copies, so it can't change.
public sealed class ClassDefinition
{
    private readonly Dictionary<int, StatBlock> _baseStatsByLevel = new();

    public ClassDefinition(ClassId id, IReadOnlyDictionary<int, StatBlock> baseStatsByLevel)
    {
        Id = id;
        foreach (KeyValuePair<int, StatBlock> row in baseStatsByLevel)
        {
            _baseStatsByLevel.Add(row.Key, row.Value.Clone());
        }
    }

    public ClassId Id { get; }

    // A copy of this level's base stats, ready to build a StatSheet from.
    public StatBlock BaseStats(int level)
    {
        if (!_baseStatsByLevel.TryGetValue(level, out StatBlock? stats))
        {
            throw new ArgumentException($"Class '{Id.Value}' has no base stats for level {level}.", nameof(level));
        }

        return stats.Clone();
    }
}
