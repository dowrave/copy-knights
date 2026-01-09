using UnityEngine;
using System.Collections;

public class DualBladeAttackController : OperatorAttackController
{
    private float _delayBetweenAttacks = 0.15f;

    public override void PerformAction(UnitEntity target, float damage)
    {
        // 쿨다운과 모션 시간 설정 (부모 로직과 동일하거나 커스텀 가능)
        SetActionDuration(); 
        SetActionCooldown();

        // Operator 본체에게 코루틴 실행 요청
        _owner.StartCoroutine(DoubleAttackCoroutine(target, damage));
    }

    private IEnumerator DoubleAttackCoroutine(UnitEntity target, float damage)
    {
        float polishedDamage = Mathf.Floor(damage);
        Vector3 targetPos = target != null ? target.transform.position : _owner.transform.position;

        // 1타 (부모의 기본 공격 로직 활용)
        base.PerformAction(target, polishedDamage);

        yield return new WaitForSeconds(_delayBetweenAttacks);

        // 2타
        if (target != null && target.Health.CurrentHealth > 0)
        {
            base.PerformAction(target, polishedDamage);
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