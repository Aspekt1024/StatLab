using StatLab.Core.Combat;

namespace StatLab.Sim.Duels;

// How a duel ended. Winner is null for a draw: the step cap was hit, or no one is left standing.
// Steps counts accepted commands (attacks), not attempts.
public readonly record struct DuelResult(Team? Winner, int Steps);
