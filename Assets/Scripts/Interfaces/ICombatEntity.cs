// ���� ���� ����� ���� ��ƼƼ�� ���� �������̽�
//using System.Numerics;
using UnityEngine;

public interface ICombatEntity
{
    public AttackType AttackType { get; }
    public AttackRangeType AttackRangeType { get; }
    float AttackPower { get; }
    float AttackSpeed { get; }

    // AttackDuration < AttackCooldown���� ª�ƾ� ��
    float AttackCooldown { get; }
    float AttackDuration { get; } // ���� ��� �ð�
    UnitEntity? CurrentTarget { get; }

    void Attack(UnitEntity target, float damage);
    bool CanAttack(); // ���� ���� ���� : ���� ���� ���� ���� �ִ°� + ���� ��Ÿ���ΰ� �� ����
    void SetAttackCooldown(float? intentionalCooldown);
    void SetAttackDuration(float? intentionalDuration);
    void UpdateAttackCooldown();
    void UpdateAttackDuration();
    void SetCurrentTarget(); // ���� ���� ��� ����
    // void RemoveCurrentTarget(); // ���� ���� ��� ����
    void NotifyTarget(); // ���� ��󿡰� �ڽ��� �����ϰ� ������ �˸�
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
