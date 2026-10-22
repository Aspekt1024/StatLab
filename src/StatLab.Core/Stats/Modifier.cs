namespace StatLab.Core.Stats;

// Changes one stat, from one source. Value is 12 for Flat, 0.05 for +5%; negative for debuffs.
public readonly record struct Modifier(StatId Stat, ModifierType Type, double Value, SourceId Source);
