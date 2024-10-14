using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ����ü ���� Ŭ����
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage;
    public bool showDamage;
    public AttackType attackType;
    private UnitEntity attacker;
    private UnitEntity target;
    private Vector3 lastKnownPosition; // ���������� �˷��� ���� ��ġ
    private string poolTag;
    public string PoolTag { get; private set; }
    private bool isMarkedForRemoval;

    public void Initialize(UnitEntity attacker, UnitEntity target, AttackType attackType, float damage, bool showDamage, string poolTag)
    {
        this.attacker = attacker;
        this.target = target;
        this.attackType = attackType;
        this.damage = damage;
        this.showDamage = showDamage;
        this.poolTag = poolTag;
        lastKnownPosition = target.transform.position;
        isMarkedForRemoval = false; 
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
            if (showDamage == true)
            {
                ObjectPoolManager.Instance.ShowDamagePopup(target.transform.position, damage);
            }

            target.TakeDamage(attackType, damage, attacker);
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
