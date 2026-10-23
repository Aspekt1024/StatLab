using StatLab.Core.Rulesets;

namespace StatLab.Core.Stats;

// Calculates every final stat, in the fixed phase order from docs/reference/stat-pipeline.md:
// 1. primaries through the pipeline, 2. ruleset conversions, 3. derived stats through the pipeline.
// Each stat is clamped by the ruleset right after its pipeline, so conversions see clamped primaries.
public static class StatCalculator
{
    public static StatBlock Calculate(StatBlock bases, ModifierSet modifiers, IRuleset ruleset)
    {
        StatBlock final = new();

        for (var i = 0; i < StatIds.Count; i++)
        {
            StatId stat = (StatId)i;
            if (StatIds.IsPrimary(stat))
            {
                var value = StatPipeline.Calculate(bases[stat], modifiers.For(stat));
                final[stat] = ruleset.Clamp(stat, value);
            }
        }

        // Convert every derived stat before writing any, so the ruleset only ever sees final primaries.
        var converted = new double[StatIds.Count];
        for (var i = 0; i < StatIds.Count; i++)
        {
            StatId stat = (StatId)i;
            if (!StatIds.IsPrimary(stat))
            {
                converted[i] = ruleset.Convert(stat, final);
            }
        }

        for (var i = 0; i < StatIds.Count; i++)
        {
            StatId stat = (StatId)i;
            if (!StatIds.IsPrimary(stat))
            {
                var value = StatPipeline.Calculate(bases[stat] + converted[i], modifiers.For(stat));
                final[stat] = ruleset.Clamp(stat, value);
            }
        }

        return final;
    }
}
