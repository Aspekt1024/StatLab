using StatLab.Core.Combat;
using StatLab.Core.Stats;
using StatLab.Core.Tests.Rulesets;
using StatLab.Core.Tests.Stats;

namespace StatLab.Core.Tests.Combat;

public class CombatStateTests
{
    private const double Tolerance = 1e-9;
    private const ulong Seed = 7;
    private static readonly CombatantId Warrior = new(1);
    private static readonly CombatantId Goblin = new(2);

    private static Combatant Make(CombatantId id, double attackPower = 30)
    {
        return new Combatant(id, 1, new StatSheet(
            new StatBlock { [StatId.Stamina] = 10, [StatId.AttackPower] = attackPower }, TestRuleset.Instance));
    }

    [Fact]
    public void Changing_a_combatant_after_creating_the_state_does_not_change_the_state()
    {
        Combatant goblin = Make(Goblin);
        CombatState state = new(TestRuleset.Instance, Seed, [Make(Warrior), goblin]);

        goblin.Health = 1;

        Assert.Equal(100, state.Find(Goblin)!.Health, Tolerance);
    }

    // Rule 5: stats built with one ruleset must not be fought with another.
    [Fact]
    public void Combatant_built_with_a_different_ruleset_is_rejected()
    {
        Combatant warrior = Make(Warrior);

        Assert.Throws<ArgumentException>(() => new CombatState(TestVerticalRuleset.Instance, Seed, [warrior]));
    }

    [Fact]
    public void Repeated_combatant_id_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new CombatState(TestRuleset.Instance, Seed, [Make(Warrior), Make(Warrior)]));
    }

    [Fact]
    public void Find_returns_null_for_an_unknown_id()
    {
        CombatState state = new(TestRuleset.Instance, Seed, [Make(Warrior)]);

        Assert.Null(state.Find(Goblin));
    }

    [Fact]
    public void Changing_a_clone_does_not_change_the_original()
    {
        CombatState original = new(TestRuleset.Instance, Seed, [Make(Warrior), Make(Goblin)]);
        CombatState clone = original.Clone();

        CombatRules.Apply(clone, new AttackCommand(Warrior, Goblin));

        Assert.Equal(100, original.Find(Goblin)!.Health, Tolerance);
        Assert.Equal(0, original.Step);
    }

    [Fact]
    public void CopyFrom_restores_health_and_step()
    {
        CombatState state = new(TestRuleset.Instance, Seed, [Make(Warrior), Make(Goblin)]);
        CombatState snapshot = state.Clone();
        CombatRules.Apply(state, new AttackCommand(Warrior, Goblin));

        state.CopyFrom(snapshot);

        Assert.Equal(100, state.Find(Goblin)!.Health, Tolerance);
        Assert.Equal(0, state.Step);
    }
}
