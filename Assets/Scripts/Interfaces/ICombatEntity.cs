// 전투 관련 기능을 가진 엔티티를 위한 인터페이스
//using System.Numerics;
using UnityEngine;
using UnityEngine.UIElements;

public interface ICombatEntity
{
    AttackType AttackType { get; }
    AttackRangeType AttackRangeType { get; }
    float AttackPower { get; }
    float AttackSpeed { get; }

    // AttackDuration < AttackCooldown보다 짧아야 함
    float AttackCooldown { get; }
    float AttackDuration { get; } // 공격 모션 시간
    UnitEntity? CurrentTarget { get; }

    void Attack(UnitEntity target, float damage);
    bool CanAttack(); // 공격 가능 여부 : 공격 범위 내에 적이 있는가 + 공격 쿨타임인가 로 결정
    void SetAttackCooldown(float? intentionalCooldown);
    void UpdateAttackCooldown();
    void SetAttackDuration();
    void UpdateAttackDuration();
    void SetCurrentTarget(); // 현재 공격 대상 설정
    void RemoveCurrentTarget(); // 현재 공격 대상 제거
    void NotifyTarget(); // 공격 대상에게 자신이 공격하고 있음을 알림

    public readonly struct AttackSource
    {
        public Vector3 Position { get; }
        public bool IsProjectile { get; }
        public GameObject? HitEffectPrefab { get; } // 공격 적중 시 발생할 이펙트

        public AttackSource(Vector3 position, bool isProjectile, GameObject? hitEffectPrefab)
        {
            Position = position;
            IsProjectile = isProjectile;
            HitEffectPrefab = hitEffectPrefab; 
        }

        public static AttackSource FromMelee(Vector3 position, GameObject? hitEffectPrefab) 
            => new AttackSource(position, false, hitEffectPrefab);

        public static AttackSource FromRanged(Vector3 position, GameObject? hitEffectPrefab = null)
            => new AttackSource(position, true, hitEffectPrefab);
    }

}

public enum AttackType
{
    Physical,
    Magical,
    True
}

public enum AttackRangeType
{
    Melee,
    Ranged
}
