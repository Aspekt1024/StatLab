using StatLab.Core.Combat;
using StatLab.Core.Stats;
using StatLab.Core.Tests.Stats;

namespace StatLab.Core.Tests.Combat;

public class CombatantTests
{
    private const double Tolerance = 1e-9;
    private static readonly CombatantId Warrior = new(1);

    private static StatSheet SheetWithStamina(double stamina)
    {
        return new StatSheet(new StatBlock { [StatId.Stamina] = stamina }, TestRuleset.Instance);
    }

    [Fact]
    public void New_combatant_starts_at_full_health()
    {
        Combatant combatant = new(Warrior, 1, SheetWithStamina(10));

        // MaxHealth 10 × 10 = 100
        Assert.Equal(100, combatant.Health, Tolerance);
    }

    [Fact]
    public void Stats_read_the_final_values()
    {
        Combatant combatant = new(Warrior, 1, SheetWithStamina(10));

        Assert.Equal(10, combatant[StatId.Stamina], Tolerance);
        Assert.Equal(100, combatant[StatId.MaxHealth], Tolerance);
    }

    // The combatant copies the sheet, so the caller can't change its stats without going through Apply.
    [Fact]
    public void Changing_the_original_sheet_does_not_change_the_combatant()
    {
        StatSheet sheet = SheetWithStamina(10);
        Combatant combatant = new(Warrior, 1, sheet);

        sheet.SetBase(StatId.Stamina, 50);

        Assert.Equal(10, combatant[StatId.Stamina], Tolerance);
    }

    [Fact]
    public void Clone_has_the_same_id_level_health_and_stats()
    {
        Combatant original = new(Warrior, 3, SheetWithStamina(10));
        original.Health = 40;

        Combatant clone = original.Clone();

        Assert.Equal(Warrior, clone.Id);
        Assert.Equal(3, clone.Level);
        Assert.Equal(40, clone.Health, Tolerance);
        Assert.Equal(100, clone[StatId.MaxHealth], Tolerance);
    }

    [Fact]
    public void Changing_a_clone_does_not_change_the_original()
    {
        Combatant original = new(Warrior, 1, SheetWithStamina(10));
        Combatant clone = original.Clone();

        clone.Health = 25;
        clone.Level = 5;

        Assert.Equal(100, original.Health, Tolerance);
        Assert.Equal(1, original.Level);
    }

    [Fact]
    public void CopyFrom_restores_an_earlier_snapshot()
    {
        Combatant combatant = new(Warrior, 1, SheetWithStamina(10));
        Combatant snapshot = combatant.Clone();
        combatant.Health = 10;
        combatant.Level = 2;

        combatant.CopyFrom(snapshot);

        Assert.Equal(100, combatant.Health, Tolerance);
        Assert.Equal(1, combatant.Level);
    }
}
