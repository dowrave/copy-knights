using System.Collections;
using UnityEngine;

// �� Mateor�� ����Ǵ� ���
public class MeteorController : MonoBehaviour
{
    private Operator? caster;
    private Enemy? target;
    private float damage;
    private float stunDuration;
    private float waitTime;
    private float fallSpeed = 5f;
    private bool hasDamageApplied = false;
    private GameObject hitEffectPrefab = default!;
    private string hitEffectTag = string.Empty;

    public void Initialize(Operator op, Enemy target, float damage, float waitTime, float stunDuration, GameObject hitEffectPrefab, string hitEffectTag)
    {
        this.caster = op;
        this.target = target;
        this.damage = damage;
        this.waitTime = waitTime;
        this.stunDuration = stunDuration;
        this.hitEffectPrefab = hitEffectPrefab;
        this.hitEffectTag = hitEffectTag;

        // �����ڳ� Ÿ���� ���ŵȴٸ� �̰͵� ���Ž�Ű�� ���� �̺�Ʈ ���
        if (caster != null)
        {
            caster.OnOperatorDied += HandleCasterDeath;
        }
        if (target != null)
        {
            target.OnDestroyed += HandleTargetDeath;
        }

        StageManager.Instance!.OnGameEnded += DestroySelf;
        StartCoroutine(FallRoutine());
    }

    private IEnumerator FallRoutine()
    {
        yield return new WaitForSeconds(waitTime + 0.1f);

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

            // Ÿ�� �浹 ����
            if (!hasDamageApplied && Vector3.Distance(transform.position, target.transform.position) < 0.5f)
            {
                ApplyDamage();
                yield return null; // ���� �������� �ٸ� �۾� ���� ���
                break; // �ݺ��� Ż��
            }

            yield return null;
        };

        DestroySelf();
    }

    private void ApplyDamage()
    {
        if (target != null && caster != null)
        {
            // ���� ȿ�� ����
            StunEffect stunEffect = new StunEffect();
            stunEffect.Initialize(target, caster, stunDuration);
            target.AddCrowdControl(stunEffect);

            // ����� ����
            ICombatEntity.AttackSource attackSource = new ICombatEntity.AttackSource(transform.position, false, hitEffectPrefab, hitEffectTag);
            target.TakeDamage(caster, attackSource, damage);

            hasDamageApplied = true;
        }
    }

    private void HandleCasterDeath(Operator op)
    {
        DestroySelf();
    }

    private void HandleTargetDeath(UnitEntity enemy)
    {
        DestroySelf();
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }

    private void UnSubscribeEvents()
    {
        if (caster != null)
        {
            caster.OnOperatorDied -= HandleCasterDeath;
        }
        if (target != null)
        {
            target.OnDestroyed -= HandleTargetDeath;
        }

        StageManager.Instance!.OnGameEnded -= DestroySelf;
    }


    private void OnDestroy()
    {
        UnSubscribeEvents();
    }
}
