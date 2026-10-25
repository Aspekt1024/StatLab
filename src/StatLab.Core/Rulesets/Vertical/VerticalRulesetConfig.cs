namespace StatLab.Core.Rulesets.Vertical;

// Tuning numbers for VerticalRuleset (rule 4). Built in code by tests until milestone 3 loads it from data/vertical/.
public sealed record VerticalRulesetConfig(
    // Conversions: derived stat per point of primary stat
    double HealthPerStamina,
    double ManaPerIntellect,
    double AttackPowerPerStrength,
    double CritChancePerAgility,

    // Limits
    double MaxCritChance,

    // Attacks
    double BasicAttackCoefficient,
    double CritMultiplier,
    double BaseMissChance,
    double MissChancePerLevel,

    // Armour: reduction = armor / (armor + ArmorPerAttackerLevel × attackerLevel + ArmorConstant)
    double ArmorPerAttackerLevel,
    double ArmorConstant);
