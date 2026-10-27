using StatLab.Core.Combat;
using StatLab.Core.Random;
using StatLab.Core.Stats;

namespace StatLab.Core.Rulesets.Vertical;

// Vertical scaling: numbers grow with level and gear; armour and miss chance scale with level.
// Damage uses a single roll table (miss, then crit, then hit) and spec-based scaling with no spread.
public sealed class VerticalRuleset : IRuleset
{
    private readonly VerticalRulesetConfig _config;

    public VerticalRuleset(VerticalRulesetConfig config)
    {
        _config = config;
    }

    public string Id => "vertical";

    // Conversions and limits come from data (StatRules).
    public double Convert(StatId derivedStat, StatBlock finalPrimaries)
    {
        return _config.Stats.Convert(derivedStat, finalPrimaries);
    }

    public double Clamp(StatId stat, double value)
    {
        return _config.Stats.Clamp(stat, value);
    }

    public DamageSpec BasicAttack => new(StatId.AttackPower, _config.BasicAttackCoefficient, CanMiss: true, CanCrit: true);

    // Until damage schools exist (milestone 4), armour reduces all damage.
    public AttackOutcome ResolveDamage(Combatant attacker, Combatant defender, DamageSpec spec, DeterministicRng rng)
    {
        AttackResult result = AttackResult.Hit;
        if (spec.CanMiss || spec.CanCrit)
        {
            var miss = spec.CanMiss ? MissChance(attacker.Level, defender.Level) : 0;
            var crit = spec.CanCrit ? attacker[StatId.CritChance] : 0;
            result = RollResult(rng.NextDouble(), miss, crit);
        }

        if (result == AttackResult.Miss)
        {
            return new AttackOutcome(AttackResult.Miss, 0);
        }

        var damage = attacker[spec.ScalesWith] * spec.Coefficient;
        if (result == AttackResult.Critical)
        {
            damage *= _config.CritMultiplier;
        }

        damage *= 1 - ArmorReduction(defender[StatId.Armor], attacker.Level);
        return new AttackOutcome(result, Math.Max(damage, 0));
    }

    // Base chance, plus a step for each level the defender is above the attacker. Never below base.
    internal double MissChance(int attackerLevel, int defenderLevel)
    {
        var levelsAbove = Math.Max(defenderLevel - attackerLevel, 0);
        return Math.Clamp(_config.BaseMissChance + levelsAbove * _config.MissChancePerLevel, 0, 1);
    }

    // One roll against stacked ranges: [0, miss) misses, [miss, miss + crit) crits, the rest hits.
    // If miss + crit exceed 1, crit is squeezed into what's left.
    internal static AttackResult RollResult(double roll, double missChance, double critChance)
    {
        if (roll < missChance)
        {
            return AttackResult.Miss;
        }

        var crit = Math.Min(critChance, 1 - missChance);
        return roll < missChance + crit ? AttackResult.Critical : AttackResult.Hit;
    }

    // Diminishing returns: approaches but never reaches 1. The same armour counts for less against
    // higher-level attackers.
    internal double ArmorReduction(double armor, int attackerLevel)
    {
        if (armor <= 0)
        {
            return 0;
        }

        return armor / (armor + _config.ArmorPerAttackerLevel * attackerLevel + _config.ArmorConstant);
    }
}
