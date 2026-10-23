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
}
