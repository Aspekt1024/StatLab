using Newtonsoft.Json.Linq;
using StatLab.Core.Rulesets;
using StatLab.Core.Rulesets.Vertical;
using StatLab.Core.Stats;
using StatLab.Data.Json;

namespace StatLab.Data.Rulesets;

// Loads a vertical ruleset file (shape in docs/reference/rulesets.md, Stat rules) into VerticalRulesetConfig.
// Reads text, not paths, so engines can pass in their own assets. Reports every problem in one pass.
public static class VerticalRulesetLoader
{
    public const int FormatVersion = 1;

    public static LoadResult<VerticalRulesetConfig> Load(string file, string text)
    {
        List<LoadError> errors = [];
        JObject? json = JsonFields.Parse(file, text, errors);
        if (json == null)
        {
            return LoadResult<VerticalRulesetConfig>.Failure(errors);
        }

        JsonFields root = new(json, file, errors);
        root.Allow("$schema");

        // A file in another shape would produce a confusing list of field errors, so stop here.
        var version = root.Number("formatVersion");
        if (version != FormatVersion)
        {
            if (version != null)
            {
                root.FieldError("formatVersion", $"File is format {version}, expected {FormatVersion}.");
            }

            return LoadResult<VerticalRulesetConfig>.Failure(errors);
        }

        List<StatRule> rules = ReadStats(root, json, errors);
        AttackConstants? attacks = ReadAttacks(root);
        ArmorConstants? armor = ReadArmor(root);
        root.ReportUnknownFields();

        if (errors.Count > 0 || attacks == null || armor == null)
        {
            return LoadResult<VerticalRulesetConfig>.Failure(errors);
        }

        return LoadResult<VerticalRulesetConfig>.Success(new VerticalRulesetConfig(
            new StatRules(rules),
            attacks.BasicAttackCoefficient,
            attacks.CritMultiplier,
            attacks.BaseMissChance,
            attacks.MissChancePerLevel,
            armor.PerAttackerLevel,
            armor.Constant));
    }

    private static List<StatRule> ReadStats(JsonFields root, JObject json, List<LoadError> errors)
    {
        var errorsBefore = errors.Count;
        List<StatRule> rules = [];

        foreach (JsonFields entry in root.Objects("stats", required: true))
        {
            StatId? stat = entry.Stat("stat");
            var min = entry.OptionalNumber("min");
            var max = entry.OptionalNumber("max");

            List<StatSource> sources = [];
            foreach (JsonFields sourceEntry in entry.Objects("sources", required: false))
            {
                StatId? sourceStat = sourceEntry.Stat("stat");
                var rate = sourceEntry.Number("rate");
                sourceEntry.ReportUnknownFields();
                if (sourceStat != null && rate != null)
                {
                    sources.Add(new StatSource(sourceStat.Value, rate.Value));
                }
            }

            entry.ReportUnknownFields();
            if (stat != null)
            {
                rules.Add(new StatRule(stat.Value, min, max, sources));
            }
        }

        // Cross-entry checks (every stat listed, sources primary...) only make sense once each entry parsed,
        // otherwise a typo in one entry also shows up as "missing stat".
        if (errors.Count == errorsBefore && json["stats"] is JArray)
        {
            foreach (var problem in StatRules.Validate(rules))
            {
                root.FieldError("stats", problem);
            }
        }

        return rules;
    }

    private static AttackConstants? ReadAttacks(JsonFields root)
    {
        JsonFields? attacks = root.Object("attacks");
        if (attacks == null)
        {
            return null;
        }

        var coefficient = AtLeast(attacks, "basicAttackCoefficient", 0);
        var critMultiplier = AtLeast(attacks, "critMultiplier", 1);
        var baseMiss = Between(attacks, "baseMissChance", 0, 1);
        var missPerLevel = AtLeast(attacks, "missChancePerLevel", 0);
        attacks.ReportUnknownFields();

        if (coefficient == null || critMultiplier == null || baseMiss == null || missPerLevel == null)
        {
            return null;
        }

        return new AttackConstants(coefficient.Value, critMultiplier.Value, baseMiss.Value, missPerLevel.Value);
    }

    private static ArmorConstants? ReadArmor(JsonFields root)
    {
        JsonFields? armor = root.Object("armor");
        if (armor == null)
        {
            return null;
        }

        var perLevel = AtLeast(armor, "perAttackerLevel", 0);
        var constant = armor.Number("constant");
        if (constant <= 0)
        {
            armor.FieldError("constant", "Must be above 0: it keeps the armour formula from dividing by zero.");
            constant = null;
        }

        armor.ReportUnknownFields();
        return perLevel == null || constant == null ? null : new ArmorConstants(perLevel.Value, constant.Value);
    }

    private static double? AtLeast(JsonFields fields, string name, double minimum)
    {
        var value = fields.Number(name);
        if (value < minimum)
        {
            fields.FieldError(name, $"Must be at least {minimum}.");
            return null;
        }

        return value;
    }

    private static double? Between(JsonFields fields, string name, double minimum, double maximum)
    {
        var value = fields.Number(name);
        if (value < minimum || value > maximum)
        {
            fields.FieldError(name, $"Must be between {minimum} and {maximum}.");
            return null;
        }

        return value;
    }

    private sealed record AttackConstants(
        double BasicAttackCoefficient,
        double CritMultiplier,
        double BaseMissChance,
        double MissChancePerLevel);

    private sealed record ArmorConstants(double PerAttackerLevel, double Constant);
}
