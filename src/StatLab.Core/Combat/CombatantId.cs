namespace StatLab.Core.Combat;

// Identifies one combatant in a fight. Runtime-only: assigned by a counter in game state, never persisted.
public readonly record struct CombatantId(int Value);
