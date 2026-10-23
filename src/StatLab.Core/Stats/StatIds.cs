namespace StatLab.Core.Stats;

public enum StatId
{
    // Primary
    Strength,
    Agility,
    Intellect,
    Stamina,
    
    // Derived
    MaxHealth,
    MaxMana,
    AttackPower,
    CritChance,
    Armor,
}

public static class StatIds
{
    // Number of StatId values; the length of every array indexed by StatId.
    public static readonly int Count = Enum.GetValues<StatId>().Length;

    // Every stat is listed, so a new StatId must be classified here or IsPrimary throws an exception.
    public static readonly StatId[] PrimaryStats = [StatId.Strength, StatId.Agility, StatId.Intellect, StatId.Stamina];
    public static readonly StatId[] DerivedStats = [StatId.MaxHealth, StatId.MaxMana, StatId.AttackPower, StatId.CritChance, StatId.Armor];

    public static bool IsPrimary(StatId stat)
    {
        if (PrimaryStats.Contains(stat)) return true;
        if (DerivedStats.Contains(stat)) return false;
        
        throw new ArgumentOutOfRangeException(nameof(stat), stat, "Stat is not classified as primary or derived");
    }
}
