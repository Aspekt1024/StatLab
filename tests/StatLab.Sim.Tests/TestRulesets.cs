using StatLab.Core.Combat;
using StatLab.Core.Random;
using StatLab.Core.Rulesets;
using StatLab.Core.Rulesets.Vertical;
using StatLab.Core.Stats;

namespace StatLab.Sim.Tests;

// MaxHealth = Stamina × 10. Damage always hits for attacker[ScalesWith] × Coefficient, so duel lengths are exact.
internal sealed class AlwaysHitRuleset : IRuleset
{
    public static readonly AlwaysHitRuleset Instance = new();

    public string Id => "always-hit";

    public DamageSpec BasicAttack => new(StatId.AttackPower, 1, CanMiss: false, CanCrit: false);

    public double Convert(StatId derivedStat, StatBlock finalPrimaries)
    {
        return derivedStat == StatId.MaxHealth ? finalPrimaries[StatId.Stamina] * 10 : 0;
    }

    public double Clamp(StatId stat, double value)
    {
        return value;
    }

    public AttackOutcome ResolveDamage(Combatant attacker, Combatant defender, DamageSpec spec, DeterministicRng rng)
    {
        return new AttackOutcome(AttackResult.Hit, attacker[spec.ScalesWith] * spec.Coefficient);
    }
}

// A VerticalRuleset with round numbers, for duels that roll real misses and crits.
internal static class TestVerticalRuleset
{
    public static readonly VerticalRuleset Instance = new(new VerticalRulesetConfig(
        new StatRules(
        [
            new(StatId.Strength, 0, null, []),
            new(StatId.Agility, 0, null, []),
            new(StatId.Intellect, 0, null, []),
            new(StatId.Stamina, 0, null, []),
            new(StatId.MaxHealth, 1, null, [new(StatId.Stamina, 10)]),
            new(StatId.MaxMana, 0, null, [new(StatId.Intellect, 15)]),
            new(StatId.AttackPower, 0, null, [new(StatId.Strength, 2)]),
            new(StatId.CritChance, 0, 0.75, [new(StatId.Agility, 0.01)]),
            new(StatId.Armor, 0, null, []),
        ]),
        BasicAttackCoefficient: 1,
        CritMultiplier: 2,
        BaseMissChance: 0.05,
        MissChancePerLevel: 0.01,
        ArmorPerAttackerLevel: 50,
        ArmorConstant: 100));
}
