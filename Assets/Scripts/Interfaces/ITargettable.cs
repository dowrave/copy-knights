using System.Collections.Generic;
using UnityEngine;
using static ICombatEntity;


/// <summary>
/// 공격 대상이 될 수 있는 객체에 구현
/// </summary>
public interface ITargettable { 

    //IReadOnlyList<ICombatEntity> AttackingEntities { get; } // 나를 공격하는 적들

    void AddAttackingEntity(ICombatEntity attacker); // 나를 공격하는 적 추가
    void RemoveAttackingEntity(ICombatEntity attacker); // 나를 공격하는 적 제거
    void TakeDamage(AttackSource attackSource, bool playGetHitEffect = true); // 공격을 받았을 때

}
