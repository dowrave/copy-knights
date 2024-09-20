// ���� ���� ����� ���� ��ƼƼ�� ���� �������̽�
public interface ICombatEntity
{
    AttackType AttackType { get; }
    AttackRangeType AttackRangeType { get; }
    float AttackPower { get; }
    float AttackSpeed { get; }
    float AttackCooldown { get; }
    UnitEntity CurrentTarget { get; }
    

    void Attack(UnitEntity target, AttackType attackType, float damage);
    bool CanAttack(); // ���� ���� ���� : ���� ���� ���� ���� �ִ°� + ���� ��Ÿ���ΰ� �� ����
    void SetAttackCooldown();
    void UpdateAttackCooldown();
    void SetCurrentTarget(); // ���� ���� ��� ����
    void DeleteCurrentTarget(); // ���� ���� ��� ����
    void NotifyTarget(); // ���� ��󿡰� �ڽ��� �����ϰ� ������ �˸�

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
