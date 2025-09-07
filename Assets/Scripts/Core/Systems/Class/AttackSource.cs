using UnityEngine;

public readonly struct AttackSource
{
    public Vector3 Position { get; }
    public bool IsProjectile { get; }
    public GameObject? HitEffectPrefab { get; }
    public string? HitEffectTag { get; }

    public float Damage { get; }
    public AttackType Type { get; }
    public UnitEntity Attacker { get; } 
    public bool ShowDamagePopup { get; }

    public AttackSource(UnitEntity attacker, Vector3 position, float damage, AttackType type, bool isProjectile, GameObject? hitEffectPrefab, string? hitEffectTag, bool showDamagePopup)
    {
        Attacker = attacker;
        Position = position;
        Damage = damage;
        Type = type;
        IsProjectile = isProjectile;
        HitEffectPrefab = hitEffectPrefab;
        HitEffectTag = hitEffectTag;
        ShowDamagePopup = showDamagePopup;
    }
}