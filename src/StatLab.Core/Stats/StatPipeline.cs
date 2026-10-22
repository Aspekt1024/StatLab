namespace StatLab.Core.Stats;

// Applies modifiers to one stat in the fixed order from docs/reference/stat-pipeline.md:
// final = (base + Σ Flat) × (1 + Σ PercentAdd) × Π(1 + each PercentMult)
// The caller passes only this stat's modifiers; Modifier.Stat is not checked. No caps or clamping.
public static class StatPipeline
{
    public static double Calculate(double baseValue, ReadOnlySpan<Modifier> modifiers)
    {
        var flat = 0.0;
        var percentAdd = 0.0;
        var percentMult = 1.0;

        foreach (Modifier modifier in modifiers)
        {
            switch (modifier.Type)
            {
                case ModifierType.Flat:
                    flat += modifier.Value;
                    break;
                case ModifierType.PercentAdd:
                    percentAdd += modifier.Value;
                    break;
                case ModifierType.PercentMult:
                    percentMult *= 1 + modifier.Value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modifiers), modifier.Type, "Unknown modifier type");
            }
        }

        return (baseValue + flat) * (1 + percentAdd) * percentMult;
    }
}
