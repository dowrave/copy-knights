using System.Collections;
using UnityEngine;

// �� Mateor�� ����Ǵ� ���
public class MeteorController : MonoBehaviour, IPooledObject
{
    private Operator? caster;
    private Enemy? target;
    private float damage;
    private float stunDuration;
    private float fallSpeed;
    private GameObject hitEffectPrefab = default!;
    private string hitEffectTag = string.Empty;
    private string poolTag = string.Empty;

    public void OnObjectSpawn(string tag)
    {
        this.poolTag = tag;
    }

    public void Initialize(Operator op, Enemy target, float damage, float fallSpeed, float stunDuration, GameObject hitEffectPrefab, string hitEffectTag)
    {
        this.caster = op;
        this.target = target;
        this.damage = damage;
        this.fallSpeed = fallSpeed;
        this.stunDuration = stunDuration;
        this.hitEffectPrefab = hitEffectPrefab;
        this.hitEffectTag = hitEffectTag;

        StartCoroutine(FallRoutine());
    }

    private IEnumerator FallRoutine()
    {
        bool hasDamageApplied = false;

        while (target != null &&
                !hasDamageApplied &&
                transform.position.y > target.transform.position.y)
        {
            // Ÿ���� ��� ����, y ��ǥ�� ����
            Vector3 targetPos = target.transform.position;
            transform.position = new Vector3(
                targetPos.x,
                transform.position.y - (fallSpeed * Time.deltaTime),
                targetPos.z
            );

            yield return null;

            // Ÿ�� �浹 ���� -> �ݶ��̴� ������� ����
            // if (Vector3.Distance(transform.position, target.transform.position) < 0.5f)
            // {
            //     ApplyDamage();
            //     hasDamageApplied = true;
            // }

        }

        ReturnToPool();
    }

    private void ApplyDamage()
    {
        if (target != null && caster != null)
        {
            // ���� ȿ�� ����
            StunBuff stunBuff = new StunBuff(stunDuration);
            target.AddBuff(stunBuff);

            // ����� ����
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
        // ���뿡 ���� �ݶ��̴��� ���ؼ��� ����Ǿ�� ��. �̰� ������ ��Ÿ� �ݶ��̴��� �浹���� ���� ������
        // �� �κ��� ���̾� & �浹 ��Ʈ������ �̿��ϸ� 
        BodyColliderController bodyCollider = other.GetComponent<BodyColliderController>();
        
        // ��ǥ�� ������ ����
        if (bodyCollider != null && bodyCollider.ParentUnit == target)
        {
            ApplyDamage();
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        ObjectPoolManager.Instance?.ReturnToPool(poolTag, gameObject);
    }
}
