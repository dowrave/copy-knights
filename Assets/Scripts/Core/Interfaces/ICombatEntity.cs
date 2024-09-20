// 전투 관련 기능을 가진 엔티티를 위한 인터페이스
public interface ICombatEntity
{
    AttackType AttackType { get; }
    AttackRangeType AttackRangeType { get; }
    float AttackPower { get; }
    float AttackSpeed { get; }
    float AttackCooldown { get; }
    UnitEntity CurrentTarget { get; }
    

    void Attack(UnitEntity target, AttackType attackType, float damage);
    bool CanAttack(); // 공격 가능 여부 : 공격 범위 내에 적이 있는가 + 공격 쿨타임인가 로 결정
    void SetAttackCooldown();
    void UpdateAttackCooldown();
    void SetCurrentTarget(); // 현재 공격 대상 설정
    void DeleteCurrentTarget(); // 현재 공격 대상 제거
    void NotifyTarget(); // 공격 대상에게 자신이 공격하고 있음을 알림

    void InitializeProjectilePool();

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
