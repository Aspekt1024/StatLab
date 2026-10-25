using StatLab.Core.Combat;
using StatLab.Core.Rulesets;
using StatLab.Core.Stats;
using StatLab.Core.Tests.Rulesets;

namespace StatLab.Core.Tests.Combat;

public class CombatStateChecksumTests
{
    private const ulong Seed = 7;
    private static readonly CombatantId Warrior = new(1);
    private static readonly CombatantId Goblin = new(2);
    private static readonly SourceId Sword = new(1);
    private static readonly SourceId Ring = new(2);

    private static CombatState RolledDuel(ulong seed = Seed)
    {
        IRuleset ruleset = TestVerticalRuleset.Instance;
        Combatant warrior = new(Warrior, Team.Player, 1, new StatSheet(
            new StatBlock { [StatId.Strength] = 20, [StatId.Agility] = 20, [StatId.Stamina] = 30 }, ruleset));
        Combatant goblin = new(Goblin, Team.Enemy, 1, new StatSheet(
            new StatBlock { [StatId.Strength] = 15, [StatId.Stamina] = 25, [StatId.Armor] = 50 }, ruleset));
        return new CombatState(ruleset, seed, [warrior, goblin]);
    }

    private static void TradeBlows(CombatState state, int rounds)
    {
        for (var i = 0; i < rounds; i++)
        {
            CombatRules.Apply(state, new AttackCommand(Warrior, Goblin));
            CombatRules.Apply(state, new AttackCommand(Goblin, Warrior));
        }
    }

    // A warrior whose sheet had two equal +5 Strength modifiers added in the given order.
    private static CombatState WithTiedModifiers(SourceId first, SourceId second)
    {
        IRuleset ruleset = TestVerticalRuleset.Instance;
        StatSheet sheet = new(new StatBlock { [StatId.Stamina] = 10 }, ruleset);
        sheet.AddSource(first, [new Modifier(StatId.Strength, ModifierType.Flat, 5, first)]);
        sheet.AddSource(second, [new Modifier(StatId.Strength, ModifierType.Flat, 5, second)]);
        return new CombatState(ruleset, Seed, [new Combatant(Warrior, Team.Player, 1, sheet)]);
    }

    // Golden master: pins this whole fight to one value. If it fails, something changed the outcome:
    // a formula, the modifier order, the rng or the checksum. Update the value only if the change was intended.
    [Fact]
    public void Five_rounds_of_the_rolled_duel_end_in_the_pinned_state()
    {
        CombatState state = RolledDuel();

        TradeBlows(state, rounds: 5);

        Assert.Equal(0x7BFF9C7A36B568C4UL, state.ComputeChecksum());
    }

    [Fact]
    public void Clone_has_the_same_checksum()
    {
        CombatState state = RolledDuel();
        TradeBlows(state, rounds: 3);

        CombatState clone = state.Clone();

        Assert.Equal(state.ComputeChecksum(), clone.ComputeChecksum());
    }

    [Fact]
    public void CopyFrom_restores_the_checksum()
    {
        CombatState state = RolledDuel();
        CombatState snapshot = state.Clone();
        var before = state.ComputeChecksum();
        TradeBlows(state, rounds: 3);

        state.CopyFrom(snapshot);

        Assert.Equal(before, state.ComputeChecksum());
    }

    [Fact]
    public void Checksum_changes_after_an_attack()
    {
        CombatState state = RolledDuel();
        var before = state.ComputeChecksum();

        CombatRules.Apply(state, new AttackCommand(Warrior, Goblin));

        Assert.NotEqual(before, state.ComputeChecksum());
    }

    // Only the rng differs between these states, so this shows the rng is part of the checksum.
    [Fact]
    public void Different_seeds_give_different_checksums()
    {
        Assert.NotEqual(RolledDuel(seed: 1).ComputeChecksum(), RolledDuel(seed: 2).ComputeChecksum());
    }

    // Peers that built the same state in a different order must agree.
    [Fact]
    public void Same_modifiers_added_in_a_different_order_give_the_same_checksum()
    {
        CombatState swordFirst = WithTiedModifiers(Sword, Ring);
        CombatState ringFirst = WithTiedModifiers(Ring, Sword);

        Assert.Equal(swordFirst.ComputeChecksum(), ringFirst.ComputeChecksum());
    }
}
