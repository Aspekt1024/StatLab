namespace StatLab.Core.Stats;

// Identifies one instance that produces modifiers (an equipped item, a buff application).
// Runtime-only: assigned by a counter in game state, never persisted.
public readonly record struct SourceId(int Value);
