using System.Collections;
using UnityEngine;

// 각 Mateor에 적용되는 요소
public class MeteorController : MonoBehaviour, IPooledObject
{
    private Operator? caster;
    private Enemy? target;
    private float damage;
    private float stunDuration;
    private float fallSpeed;
    private GameObject hitEffectPrefab = default!;
    private string hitEffectTag = string.Empty;
    private string objectPoolTag = string.Empty;

    public void OnObjectSpawn(string tag)
    {
    }

    public void Initialize(Operator op, Enemy target, float damage, float fallSpeed, float stunDuration, GameObject hitEffectPrefab, string hitEffectTag, string objectPoolTag)
    {
        this.caster = op;
        this.target = target;
        this.damage = damage;
        this.fallSpeed = fallSpeed;
        this.stunDuration = stunDuration;
        this.hitEffectPrefab = hitEffectPrefab;
        this.hitEffectTag = hitEffectTag;
        this.objectPoolTag = objectPoolTag;

        if (gameObject.activeInHierarchy) StartCoroutine(FallRoutine());
    }

    private IEnumerator FallRoutine()
    {
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

            // 대미지 적용
            AttackSource attackSource = new AttackSource(
                attacker: caster,
                position: transform.position,
                damage: damage,
                type: caster.AttackType,
                isProjectile: true,
                hitEffectTag: hitEffectTag,
                showDamagePopup: false
            );

            target.TakeDamage(attackSource);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 몸통에 닿은 콜라이더에 대해서만 실행되어야 함. 이게 없으면 사거리 콜라이더와 충돌했을 때도 동작함
        // 이 부분은 레이어 & 충돌 매트릭스를 이용하면 
        BodyColliderController bodyCollider = other.GetComponent<BodyColliderController>();
        
        // 목표에 닿으면 실행
        if (bodyCollider != null && bodyCollider.ParentUnit == target)
        {
            ApplyDamage();
            Logger.LogFieldStatus(hitEffectTag);
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        ObjectPoolManager.Instance?.ReturnToPool(objectPoolTag, gameObject);
    }
}
