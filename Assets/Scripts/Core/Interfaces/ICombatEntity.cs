// ���� ���� ����� ���� ��ƼƼ�� ���� �������̽�
public interface ICombatEntity
{
    
    AttackType AttackType { get; }
    AttackRangeType AttackRangeType { get; }
    float AttackPower { get; }
    float AttackSpeed { get; }

    // AttackDuration�� AttackCooldown���� ª�ƾ� �� - AttackSpeed�� ������ ������ �����ϵ�, �⺻���� �� ���� �����ϸ� �ǰڴ�
    float AttackCooldown { get; }
    float AttackDuration { get; } // ���� ��� �ð�
    UnitEntity CurrentTarget { get; }
    

    void Attack(UnitEntity target, AttackType attackType, float damage);
    bool CanAttack(); // ���� ���� ���� : ���� ���� ���� ���� �ִ°� + ���� ��Ÿ���ΰ� �� ����
    void SetAttackCooldown();
    void UpdateAttackCooldown();
    void SetAttackDuration();
    void UpdateAttackDuration();
    void SetCurrentTarget(); // ���� ���� ��� ����
    void RemoveCurrentTarget(); // ���� ���� ��� ����
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
