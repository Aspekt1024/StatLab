using Newtonsoft.Json.Linq;
using StatLab.Core.Rulesets.Vertical;
using StatLab.Core.Stats;
using StatLab.Data.Rulesets;
using StatLab.Data.Tests.Fixtures;

namespace StatLab.Data.Tests.Rulesets;

public class VerticalRulesetLoaderTests
{
    private const double Tolerance = 1e-9;
    private const string File = "vertical/ruleset.json";

    // A complete, valid file (Fixtures/vertical-ruleset.json). Each test breaks one thing.
    // MaxHealth = Stamina × 10 + Strength × 2; CritChance capped at 0.75; armour constant 100.
    private static readonly string ValidJson = Fixture.Read("vertical-ruleset.json");

    private static string With(Action<JObject> change)
    {
        JObject json = JObject.Parse(ValidJson);
        change(json);
        return json.ToString();
    }

    private static IReadOnlyList<LoadError> ErrorsFor(string text)
    {
        LoadResult<VerticalRulesetConfig> result = VerticalRulesetLoader.Load(File, text);
        Assert.False(result.Succeeded);
        return result.Errors;
    }

    private static JObject StatEntry(JObject json, string stat)
    {
        return (JObject)json["stats"]!.First(entry => (string?)entry["stat"] == stat);
    }

    [Fact]
    public void Valid_file_loads()
    {
        LoadResult<VerticalRulesetConfig> result = VerticalRulesetLoader.Load(File, ValidJson);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        VerticalRulesetConfig config = result.Value!;
        Assert.Equal(2, config.CritMultiplier, Tolerance);
        Assert.Equal(100, config.ArmorConstant, Tolerance);
    }

    [Fact]
    public void Loaded_stat_rules_convert_and_clamp()
    {
        VerticalRulesetConfig config = VerticalRulesetLoader.Load(File, ValidJson).Value!;
        StatBlock primaries = new() { [StatId.Stamina] = 10, [StatId.Strength] = 20 };

        // Stamina 10 × 10 + Strength 20 × 2 = 140
        Assert.Equal(140, config.Stats.Convert(StatId.MaxHealth, primaries), Tolerance);
        Assert.Equal(0.75, config.Stats.Clamp(StatId.CritChance, 0.9), Tolerance);
    }

    [Fact]
    public void Invalid_json_reports_its_line()
    {
        // The valid file with its final closing brace cut off
        var truncated = ValidJson.TrimEnd()[..^1];

        LoadError error = Assert.Single(ErrorsFor(truncated));

        Assert.Equal(File, error.File);
        Assert.StartsWith("line ", error.Location);
    }

    [Fact]
    public void Other_format_version_is_the_only_error()
    {
        LoadError error = Assert.Single(ErrorsFor(With(json => json["formatVersion"] = 2)));

        Assert.Contains("format 2, expected 1", error.Message);
    }

    [Fact]
    public void Missing_format_version_is_an_error()
    {
        LoadError error = Assert.Single(ErrorsFor(With(json => json.Remove("formatVersion"))));

        Assert.Contains("formatVersion", error.Message);
    }

    [Fact]
    public void Unknown_field_is_an_error()
    {
        LoadError error = Assert.Single(ErrorsFor(With(json => json["attacks"]!["critMultiplierr"] = 2)));

        Assert.Contains("critMultiplierr", error.Message);
        Assert.StartsWith("attacks.critMultiplierr", error.Location);
    }

    [Fact]
    public void Errors_name_the_path_and_line()
    {
        LoadError error = Assert.Single(ErrorsFor(With(json => json["armor"]!["constant"] = 0)));

        Assert.Matches(@"^armor\.constant \(line \d+\)$", error.Location);
    }

    [Theory]
    [InlineData("Haste")]
    [InlineData("maxhealth")]
    [InlineData("4")]
    public void Stat_names_must_match_exactly(string name)
    {
        LoadError error = Assert.Single(ErrorsFor(With(json => StatEntry(json, "Armor")["stat"] = name)));

        Assert.Contains($"Unknown stat '{name}'", error.Message);
    }

    [Fact]
    public void Number_given_as_text_is_an_error()
    {
        LoadError error = Assert.Single(ErrorsFor(With(json => json["attacks"]!["critMultiplier"] = "2")));

        Assert.Contains("number", error.Message);
    }

    [Fact]
    public void Missing_required_field_is_an_error()
    {
        LoadError error = Assert.Single(ErrorsFor(With(json => ((JObject)json["armor"]!).Remove("perAttackerLevel"))));

        Assert.Contains("perAttackerLevel", error.Message);
    }

    // StatRules' own checks come through as load errors.
    [Fact]
    public void Missing_stat_entry_is_an_error()
    {
        LoadError error = Assert.Single(ErrorsFor(With(json => StatEntry(json, "Armor").Remove())));

        Assert.Contains("Armor is missing", error.Message);
    }

    [Theory]
    [InlineData("armor", "constant", 0)]
    [InlineData("attacks", "critMultiplier", 0.5)]
    [InlineData("attacks", "baseMissChance", 1.5)]
    [InlineData("attacks", "missChancePerLevel", -0.01)]
    public void Constants_out_of_range_are_errors(string section, string field, double value)
    {
        LoadError error = Assert.Single(ErrorsFor(With(json => json[section]![field] = value)));

        Assert.StartsWith($"{section}.{field}", error.Location);
    }

    [Fact]
    public void Every_problem_is_reported_at_once()
    {
        var text = With(json =>
        {
            json["armor"]!["constant"] = 0;
            json["attacks"]!["critMultiplier"] = "2";
            json["extra"] = true;
        });

        Assert.Equal(3, ErrorsFor(text).Count);
    }
}
