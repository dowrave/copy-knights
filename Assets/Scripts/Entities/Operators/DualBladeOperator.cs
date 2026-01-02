using UnityEngine;
using System.Collections;

public class DualBladeOperator : Operator
{
    // 공격 사이의 간격
    private float delayBetweenAttacks = 0.15f;

    public override void Attack(UnitEntity target, float damage)
    {
        // 2회 공격 로직을 코루틴으로 구현
        StartCoroutine(DoubleAttackCoroutine(target, damage));

        // 공격 모션 중이라는 시간 설정
        SetAttackDuration();

        // 공격 쿨다운 설정
        SetAttackCooldown();
    }

    private IEnumerator DoubleAttackCoroutine(UnitEntity target, float damage)
    {
        bool showDamagePopup = false;
        float polishedDamage = Mathf.Floor(damage);
        Vector3 targetPosition = target.transform.position;

        base.PerformAttack(target, polishedDamage, showDamagePopup);

        yield return new WaitForSeconds(delayBetweenAttacks);

        if (target != null && target.Health.CurrentHealth > 0)
        {
            base.PerformAttack(target, polishedDamage, showDamagePopup);
        }
        else
        {
            // 타겟이 죽었으면 헛스윙 - SP 회복 등의 동작은 이뤄지지 않음
            AttackSource missAttackSource = new AttackSource(
                attacker: this,
                position: transform.position,
                damage: 0f,
                type: AttackType,
                isProjectile: false,
                hitEffectTag: null,
                showDamagePopup: false
            );
            
            base.PlayMeleeAttackEffect(targetPosition, missAttackSource);
        }
    }
}