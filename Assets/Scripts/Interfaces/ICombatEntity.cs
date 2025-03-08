// ���� ���� ����� ���� ��ƼƼ�� ���� �������̽�
//using System.Numerics;
using UnityEngine;
using UnityEngine.UIElements;

public interface ICombatEntity
{
    AttackType AttackType { get; }
    AttackRangeType AttackRangeType { get; }
    float AttackPower { get; }
    float AttackSpeed { get; }

    // AttackDuration < AttackCooldown���� ª�ƾ� ��
    float AttackCooldown { get; }
    float AttackDuration { get; } // ���� ��� �ð�
    UnitEntity? CurrentTarget { get; }

    void Attack(UnitEntity target, float damage);
    bool CanAttack(); // ���� ���� ���� : ���� ���� ���� ���� �ִ°� + ���� ��Ÿ���ΰ� �� ����
    void SetAttackCooldown(float? intentionalCooldown);
    void UpdateAttackCooldown();
    void SetAttackDuration();
    void UpdateAttackDuration();
    void SetCurrentTarget(); // ���� ���� ��� ����
    void RemoveCurrentTarget(); // ���� ���� ��� ����
    void NotifyTarget(); // ���� ��󿡰� �ڽ��� �����ϰ� ������ �˸�

    public readonly struct AttackSource
    {
        public Vector3 Position { get; }
        public bool IsProjectile { get; }
        public GameObject? HitEffectPrefab { get; } // ���� ���� �� �߻��� ����Ʈ

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
