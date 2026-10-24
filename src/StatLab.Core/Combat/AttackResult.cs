namespace StatLab.Core.Combat;

// How an attack landed. Shared by every ruleset; a ruleset only produces the kinds it uses.
public enum AttackResult
{
    Miss,
    Hit,
    Critical,
}
