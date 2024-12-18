using UnityEngine;

public interface ICombatData 
{
    AttackType AttackType { get; }
    AttackRangeType AttackRangeType { get; }
    GameObject HitEffectPrefab { get; }
}
