using System.Collections;
using UnityEngine;

// �� Mateor�� ����Ǵ� ���
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
            // Ÿ���� ��� ����, y ��ǥ�� ����
            Vector3 targetPos = target.transform.position;
            transform.position = new Vector3(
                targetPos.x,
                transform.position.y - (fallSpeed * Time.deltaTime),
                targetPos.z
            );

            // Ÿ�� �浹 ����
            if (!hasDamageApplied && Vector3.Distance(transform.position, target.transform.position) < 0.5f)
            {
                ApplyDamage();
                yield return null; // ���� �������� �ٸ� �۾� ���� ���
                break; // �ݺ��� Ż��
            }

            yield return null;
        };

        Destroy(gameObject);
    }

    private void ApplyDamage()
    {
        if (target != null)
        {
            // ����� ����
            ICombatEntity.AttackSource attackSource = new ICombatEntity.AttackSource(transform.position, false, hitEffectPrefab);
            target.TakeDamage(caster, attackSource, damage);

            // ���� ȿ�� ����
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
