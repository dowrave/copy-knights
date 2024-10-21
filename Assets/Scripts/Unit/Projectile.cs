using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ����ü ���� Ŭ����
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float value;
    public bool showValue;
    public AttackType attackType;
    private bool isHealing = false;
    private UnitEntity attacker;
    private UnitEntity target;
    private Vector3 lastKnownPosition; // ���������� �˷��� ���� ��ġ
    private string poolTag;
    public string PoolTag { get; private set; }
    private bool isMarkedForRemoval;

    public void Initialize(UnitEntity attacker, UnitEntity target, AttackType attackType, float value, bool showValue, string poolTag)
    {
        this.attacker = attacker;
        this.target = target;
        this.attackType = attackType;
        this.value = value;
        this.showValue = showValue;
        this.poolTag = poolTag;
        lastKnownPosition = target.transform.position;
        isMarkedForRemoval = false;

        // �����ڿ� ����� ���ٸ� ���� ����
        if (attacker.Faction == target.Faction)
        {
            isHealing = true;
            this.showValue = true;
        }
    }

    private void Update()
    {
        if (target == null)
        {
            ReturnToPool();
            return;
        }

        // Ÿ���� ��� �ִٸ� ��ġ ����
        lastKnownPosition = target.transform.position;

        // ���������� �˷��� ��ġ�� �̵�
        Vector3 direction = (lastKnownPosition - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // ��ǥ ���� ���� Ȯ��
        if (Vector3.Distance(transform.position, lastKnownPosition) < 0.1f)
        {
            OnReachTarget();
        }
    }

    private void OnReachTarget()
    {
        if (target != null)
        {

            if (isHealing)
            {
                target.TakeHeal(value, attacker);
            }
            else
            {
                target.TakeDamage(attackType, value, attacker);

                // ������� ������ �ϴ� ��쿡�� ������
                if (showValue == true)
                {
                    ObjectPoolManager.Instance.ShowFloatingText(target.transform.position, value, false);
                }
            }
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        ObjectPoolManager.Instance.ReturnToPool(poolTag, gameObject);
    }

    // Ǯ���� ����� �� ȣ��� �޼���
    private void OnDisable()
    {
        target = null;
        lastKnownPosition = Vector3.zero;
    }

    public void MarkPoolForRemoval()
    {
        isMarkedForRemoval = true;
    }
}
