// ���� ���� ����� ���� ��ƼƼ�� ���� �������̽�
//using System.Numerics;
using UnityEngine;

public interface ICombatEntity
{
    AttackType AttackType { get; }
    AttackRangeType AttackRangeType { get; }
    float AttackPower { get; }
    float AttackSpeed { get; }

    // AttackDuration < AttackCooldown���� ª�ƾ� ��
    float AttackCooldown { get; }
    float AttackDuration { get; } // ���� ��� �ð�
    UnitEntity CurrentTarget { get; }

    void Attack(UnitEntity target, float damage);
    bool CanAttack(); // ���� ���� ���� : ���� ���� ���� ���� �ִ°� + ���� ��Ÿ���ΰ� �� ����
    void SetAttackCooldown();
    void UpdateAttackCooldown();
    void SetAttackDuration();
    void UpdateAttackDuration();
    void SetCurrentTarget(); // ���� ���� ��� ����
    void RemoveCurrentTarget(); // ���� ���� ��� ����
    void NotifyTarget(); // ���� ��󿡰� �ڽ��� �����ϰ� ������ �˸�

    void InitializeProjectilePool();

    public readonly struct AttackSource
    {
        public Vector3 Position { get; }
        public bool IsProjectile { get; }

        public AttackSource(Vector3 position, bool isProjectile)
        {
            Position = position;
            IsProjectile = isProjectile;
        }

        public static AttackSource FromMelee(Vector3 position) => new AttackSource(position, false);
        public static AttackSource FromRanged(Vector3 position) => new AttackSource(position, true);
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
