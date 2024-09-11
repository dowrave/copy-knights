// 전투 관련 기능을 가진 엔티티를 위한 인터페이스
public interface ICombatEntity
{
    AttackType AttackType { get; }
    AttackRangeType AttackRangeType { get; }
    float AttackPower { get; }
    float AttackSpeed { get; }
    float AttackCooldown { get; }
    

    void Attack(UnitEntity target, AttackType attackType, float damage);
    bool CanAttack(); // 공격 가능 여부 : 공격 범위 내에 적이 있는가 + 공격 쿨타임인가 로 결정
    void SetAttackCooldown();
    void UpdateAttackCooldown();
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
