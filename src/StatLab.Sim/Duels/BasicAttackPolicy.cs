using StatLab.Core.Combat;

namespace StatLab.Sim.Duels;

// Always basic-attacks the first opponent still standing, in turn order.
public sealed class BasicAttackPolicy : ICombatPolicy
{
    public static readonly BasicAttackPolicy Instance = new();

    public Command ChooseCommand(CombatState state, CombatantId self)
    {
        Combatant me = state.Find(self) ?? throw new ArgumentException($"Combatant {self.Value} isn't in this fight.", nameof(self));

        foreach (Combatant other in state.Combatants)
        {
            if (other.Team != me.Team && !other.IsDefeated)
            {
                return new AttackCommand(self, other.Id);
            }
        }

        throw new InvalidOperationException("No opponent is standing; the fight is already over.");
    }
}
