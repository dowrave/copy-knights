// 전투 관련 기능을 가진 엔티티를 위한 인터페이스
public interface ICombatEntity
{
    AttackType AttackType { get; }
    AttackRangeType AttackRangeType { get; }
    float AttackPower { get; }
    float AttackSpeed { get; }
    float AttackCooldown { get; }
    

    void Attack(UnitEntity target);
    bool CanAttack(UnitEntity target);
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
