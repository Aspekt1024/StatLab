using StatLab.Core.Combat;

namespace StatLab.Sim.Duels;

// Chooses what a combatant does on its turn. The sim's stand-in for player input and enemy AI.
// A policy that needs randomness keeps its own seeded rng: drawing from the fight's would shift its combat rolls.
public interface ICombatPolicy
{
    Command ChooseCommand(CombatState state, CombatantId self);
}
