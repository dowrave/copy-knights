using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ����ü ���� Ŭ����
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage;
    public AttackType attackType;
    private UnitEntity target;
    private Vector3 lastKnownPosition; // ���������� �˷��� ���� ��ġ

    public void Initialize(UnitEntity target, AttackType attackType, float damage)
    {
        this.target = target;
        this.attackType = attackType;
        this.damage = damage;
        lastKnownPosition = target.transform.position;
    }

    private void Update()
    {
        if (target != null)
        {
            // Ÿ���� ��� �ִٸ� ��ġ ����
            lastKnownPosition = target.transform.position;
        }

        // ���������� �˷��� ��ġ�� �̵�
        Vector3 direction = (lastKnownPosition - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // ��ǥ ���� ���� Ȯ��
        if (Vector3.Distance(transform.position, lastKnownPosition) < 0.1f)
        {
            if (target != null)
            {
                // Ÿ���� ��� �ִٸ� ������� ����
                target.TakeDamage(attackType, damage);
            }
            Destroy(gameObject);
        }
    }

}
