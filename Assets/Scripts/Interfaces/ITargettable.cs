using System.Collections.Generic;
using UnityEngine;
using static ICombatEntity;


/// <summary>
/// ���� ����� �� �� �ִ� ��ü�� ����
/// </summary>
public interface ITargettable { 

    //IReadOnlyList<ICombatEntity> AttackingEntities { get; } // ���� �����ϴ� ����

    void AddAttackingEntity(ICombatEntity attacker); // ���� �����ϴ� �� �߰�
    void RemoveAttackingEntity(ICombatEntity attacker); // ���� �����ϴ� �� ����
    void TakeDamage(UnitEntity attacker, AttackSource attackSource, float damage); 

}
