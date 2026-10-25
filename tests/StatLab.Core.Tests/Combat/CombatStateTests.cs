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

    private static Combatant Make(CombatantId id, Team team, double attackPower = 30)
    {
        return new Combatant(id, team, 1, new StatSheet(
            new StatBlock { [StatId.Stamina] = 10, [StatId.AttackPower] = attackPower }, TestRuleset.Instance));
    }

    [Fact]
    public void Changing_a_combatant_after_creating_the_state_does_not_change_the_state()
    {
        Combatant goblin = Make(Goblin, Team.Enemy);
        CombatState state = new(TestRuleset.Instance, Seed, [Make(Warrior, Team.Player), goblin]);

        goblin.Health = 1;

        Assert.Equal(100, state.Find(Goblin)!.Health, Tolerance);
    }

    // Rule 5: stats built with one ruleset must not be fought with another.
    [Fact]
    public void Combatant_built_with_a_different_ruleset_is_rejected()
    {
        Combatant warrior = Make(Warrior, Team.Player);

        Assert.Throws<ArgumentException>(() => new CombatState(TestVerticalRuleset.Instance, Seed, [warrior]));
    }

    [Fact]
    public void Repeated_combatant_id_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new CombatState(TestRuleset.Instance, Seed, [Make(Warrior, Team.Player), Make(Warrior, Team.Player)]));
    }

    [Fact]
    public void Find_returns_null_for_an_unknown_id()
    {
        CombatState state = new(TestRuleset.Instance, Seed, [Make(Warrior, Team.Player)]);

        Assert.Null(state.Find(Goblin));
    }

    [Fact]
    public void Changing_a_clone_does_not_change_the_original()
    {
        CombatState original = new(TestRuleset.Instance, Seed, [Make(Warrior, Team.Player), Make(Goblin, Team.Enemy)]);
        CombatState clone = original.Clone();

        CombatRules.Apply(clone, new AttackCommand(Warrior, Goblin));

        Assert.Equal(100, original.Find(Goblin)!.Health, Tolerance);
        Assert.Equal(0, original.Step);
    }

    [Fact]
    public void CopyFrom_restores_health_and_step()
    {
        CombatState state = new(TestRuleset.Instance, Seed, [Make(Warrior, Team.Player), Make(Goblin, Team.Enemy)]);
        CombatState snapshot = state.Clone();
        CombatRules.Apply(state, new AttackCommand(Warrior, Goblin));

        state.CopyFrom(snapshot);

        Assert.Equal(100, state.Find(Goblin)!.Health, Tolerance);
        Assert.Equal(0, state.Step);
    }

    [Fact]
    public void CopyFrom_restores_whose_turn_it_is()
    {
        CombatState state = new(TestRuleset.Instance, Seed, [Make(Warrior, Team.Player), Make(Goblin, Team.Enemy)]);
        CombatState snapshot = state.Clone();
        CombatRules.Apply(state, new AttackCommand(Warrior, Goblin));

        state.CopyFrom(snapshot);

        Assert.Equal(Warrior, state.CurrentActor);
    }

    [Fact]
    public void Fight_with_only_one_team_is_already_over()
    {
        CombatState state = new(TestRuleset.Instance, Seed, [Make(Warrior, Team.Player)]);

        Assert.True(state.IsOver);
        Assert.Equal(Team.Player, state.Winner);
    }
}
