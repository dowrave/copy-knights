using System.Collections.Generic;

/// <summary>
/// ���� ����� �� �� �ִ� ��ü�� ����
/// </summary>
public interface ITargettable { 

    //IReadOnlyList<ICombatEntity> AttackingEntities { get; } // ���� �����ϴ� ����

    void AddAttackingEntity(ICombatEntity attacker); // ���� �����ϴ� �� �߰�
    void RemoveAttackingEntity(ICombatEntity attacker); // ���� �����ϴ� �� ����
    void TakeDamage(AttackType attacktype, float damage); 

}
