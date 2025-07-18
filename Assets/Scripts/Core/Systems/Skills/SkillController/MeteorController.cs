using System.Collections;
using UnityEngine;

// 각 Mateor에 적용되는 요소
public class MeteorController : MonoBehaviour, IPooledObject
{
    private Operator? caster;
    private Enemy? target;
    private float damage;
    private float stunDuration;
    private GameObject hitEffectPrefab = default!;
    private string hitEffectTag = string.Empty;
    private string poolTag = string.Empty;

    public void OnObjectSpawn(string tag)
    {
        this.poolTag = tag;
    }

    public void Initialize(Operator op, Enemy target, float damage, float delayTime, float stunDuration, GameObject hitEffectPrefab, string hitEffectTag)
    {
        this.caster = op;
        this.target = target;
        this.damage = damage;
        this.stunDuration = stunDuration;
        this.hitEffectPrefab = hitEffectPrefab;
        this.hitEffectTag = hitEffectTag;

        StartCoroutine(DelayedFallRoutine(delayTime));
    }
    private IEnumerator DelayedFallRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(FallRoutine());
    }

    private IEnumerator FallRoutine()
    {
        float fallSpeed = 5f;
        bool hasDamageApplied = false;

        while (target != null &&
                !hasDamageApplied &&
                transform.position.y > target.transform.position.y)
        {
            // 타겟을 계속 추적, y 좌표만 감소
            Vector3 targetPos = target.transform.position;
            transform.position = new Vector3(
                targetPos.x,
                transform.position.y - (fallSpeed * Time.deltaTime),
                targetPos.z
            );

            // 타겟 충돌 판정
            if (Vector3.Distance(transform.position, target.transform.position) < 0.5f)
            {
                ApplyDamage();
                hasDamageApplied = true;
            }

            yield return null;
        }

        ReturnToPool();
    }

    private void ApplyDamage()
    {
        if (target != null && caster != null)
        {
            // 기절 효과 적용
            StunBuff stunBuff = new StunBuff(stunDuration);
            target.AddBuff(stunBuff);
            stunBuff.OnApply(target, caster);

            // 대미지 적용
            AttackSource attackSource = new AttackSource(
                attacker: caster,
                position: transform.position,
                damage: damage,
                type: caster.AttackType,
                isProjectile: true,
                hitEffectPrefab: hitEffectPrefab,
                hitEffectTag: hitEffectTag
            );
            target.TakeDamage(attackSource);
        }
    }

    private void ReturnToPool()
    {
        ObjectPoolManager.Instance?.ReturnToPool(poolTag, gameObject);
    }
}
