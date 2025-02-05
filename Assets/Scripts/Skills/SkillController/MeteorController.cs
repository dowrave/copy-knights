using System.Collections;
using UnityEngine;

// 각 Mateor에 적용되는 요소
public class MeteorController : MonoBehaviour
{
    private Operator caster;
    private Enemy target;
    private float damage;
    private float stunDuration;
    private float waitTime;
    private float fallSpeed = 5f;
    private bool hasDamageApplied = false;
    private GameObject hitEffectPrefab; 

    public void Initialize(Operator op, Enemy target, float damage, float waitTime, float stunDuration, GameObject hitEffectPrefab)
    {
        this.caster = op;
        this.target = target;
        this.damage = damage;
        this.waitTime = waitTime;
        this.stunDuration = stunDuration;
        this.hitEffectPrefab = hitEffectPrefab;

        StartCoroutine(FallRoutine());

        if (caster != null)
        {
            caster.OnOperatorDied += HandleCasterDeath;
        }
    }

    private IEnumerator FallRoutine()
    {
        yield return new WaitForSeconds(waitTime + 0.1f);

        while (target != null && transform.position.y > target.transform.position.y)
        {
            // 타겟을 계속 추적, y 좌표만 감소
            Vector3 targetPos = target.transform.position;
            transform.position = new Vector3(
                targetPos.x,
                transform.position.y - (fallSpeed * Time.deltaTime),
                targetPos.z
            );

            // 타겟 충돌 판정
            if (!hasDamageApplied && Vector3.Distance(transform.position, target.transform.position) < 0.5f)
            {
                ApplyDamage();
                yield return null; // 현재 프레임의 다른 작업 종료 대기
                break; // 반복문 탈출
            }

            yield return null;
        };

        Destroy(gameObject);
    }

    private void ApplyDamage()
    {
        if (target != null)
        {
            // 대미지 적용
            ICombatEntity.AttackSource attackSource = new ICombatEntity.AttackSource(transform.position, false, hitEffectPrefab);
            target.TakeDamage(caster, attackSource, damage);

            // 기절 효과 적용
            StunEffect stunEffect = new StunEffect();
            stunEffect.Initialize(target, caster, stunDuration);
            target.AddCrowdControl(stunEffect);

            hasDamageApplied = true;
        }
    }

    private void HandleCasterDeath(Operator op)
    {
        if (caster != null)
        {
            caster.OnOperatorDied -= HandleCasterDeath;
        }

        Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (caster != null)
        {
            caster.OnOperatorDied -= HandleCasterDeath;
        }
    }
}
