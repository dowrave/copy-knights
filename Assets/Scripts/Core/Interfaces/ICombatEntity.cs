// ���� ���� ����� ���� ��ƼƼ�� ���� �������̽�
public interface ICombatEntity
{
    AttackType AttackType { get; }
    AttackRangeType AttackRangeType { get; }
    float AttackPower { get; }
    float AttackSpeed { get; }
    float AttackCooldown { get; }
    

    void Attack(UnitEntity target);
    bool CanAttack(); // ���� ���� ���� : ���� ���� ���� ���� �ִ°� + ���� ��Ÿ���ΰ� �� ����
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