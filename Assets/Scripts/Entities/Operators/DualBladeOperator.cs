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
    }

    private IEnumerator DoubleAttackCoroutine(UnitEntity target, float damage)
    {
        bool showDamagePopup = false;
        float polishedDamage = Mathf.Floor(damage);

        base.PerformAttack(target, polishedDamage, showDamagePopup);

        yield return new WaitForSeconds(delayBetweenAttacks);

        if (target != null && target.CurrentHealth > 0)
        {
            base.PerformAttack(target, polishedDamage, showDamagePopup);
        }
    }
}