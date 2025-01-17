using System.Collections;
using UnityEngine;

// �� Mateor�� ����Ǵ� ���
public class MeteorController : MonoBehaviour
{
    private Operator caster;
    private Enemy target;
    private float damage;
    private float stunDuration;
    private float fallSpeed = 10f;
    private bool hasDamageApplied = false;

    public void Initialize(Operator op, Enemy target, float damage, float stunDuration)
    {
        this.caster = op;
        this.target = target;
        this.damage = damage;
        this.stunDuration = stunDuration;

        StartCoroutine(FallRoutine());
    }

    private IEnumerator FallRoutine()
    {
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
            }

            yield return null;
        };

        Destroy(gameObject, 0.5f);
    }

    private void ApplyDamage()
    {
        if (target != null)
        {
            // ����� ����
            ICombatEntity.AttackSource attackSource = new ICombatEntity.AttackSource(transform.position, false);
            target.TakeDamage(caster, attackSource, damage);

            // ���� ȿ�� ����
            StunEffect stunEffect = new StunEffect();
            stunEffect.Initialize(target, caster, stunDuration);
            target.AddCrowdControl(stunEffect);

            hasDamageApplied = true;
        }
    }
}
