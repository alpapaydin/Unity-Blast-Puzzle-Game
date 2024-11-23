public enum DamageType
{
    None,
    Blast,
    TNT,
    TNTCombo
}
public static class DamageTypeExtensions
{
    public static int GetExplosionRange(this DamageType damageType)
    {
        return damageType switch
        {
            DamageType.TNT => 5,
            DamageType.TNTCombo => 7,
            _ => 1
        };
    }
}