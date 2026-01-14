using UnityEngine;
using System.Collections;

public class DualBladeAttackController : OpAttackController
{
    private float _delayBetweenAttacks = 0.15f;

    public override bool PerformAction(UnitEntity target, float value, bool showPopup = false)
    {
        // 쿨다운 수정 및 다른 모션이 나갈 경우 종료
        if (!base.PerformAction(target, value)) return false;


        float polishedDamage = Mathf.Floor(value);

        // 코루틴 내에 PerformActualAction이 있기 때문에 코루틴을 실행하는 위치는 PerformAction이 되어야 함
        // PerformActualAction이라면 재귀호출이 된다
        _owner.StartCoroutine(DoubleAttackCoroutine(target, polishedDamage));
        

        return true;
    }

    private IEnumerator DoubleAttackCoroutine(UnitEntity target, float damage)
    {
        float polishedDamage = Mathf.Floor(damage);
        Vector3 targetPos = target != null ? target.transform.position : _owner.transform.position;

        // 1타 (부모의 기본 공격 로직 활용)
        base.PerformActualAction(target, polishedDamage);

        yield return new WaitForSeconds(_delayBetweenAttacks);

        // 2타
        if (target != null && target.Health.CurrentHealth > 0)
        {
            base.PerformActualAction(target, polishedDamage);
        }
        else
        {
            // 타겟 사망 시 헛스윙 처리 (이펙트만 재생)
            // PlayMeleeAttackEffect 등도 접근 가능해야 함
            AttackSource missSource = new AttackSource(
                attacker: _owner, 
                position: _owner.transform.position, 
                damage: 0, 
                type: _owner.AttackType, 
                isProjectile: false, 
                hitEffectTag: null, 
                showDamagePopup: false
            );
            
            // _owner를 통해 이펙트 재생 (Operator에 public 메서드로 열려있다면)
            _owner.PlayMeleeAttackEffect(targetPos, missSource);
        }
    }
}