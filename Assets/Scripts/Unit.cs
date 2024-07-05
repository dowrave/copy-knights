using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Unit : MonoBehaviour
{
    [SerializeField, HideInInspector]
    protected UnitStats stats;// ���� �����͸� �����ϴ� �ʵ�
    public UnitStats Stats => stats; // Stats ������Ƽ
    // �� => �κ��� �̰Ͱ� ����.
    /*
     public UnitStats Stats
    {
        get { return stats; }
    }
     */

    protected bool canAttack = true;

    public virtual void Initialize(UnitStats initialStats)
    {
        stats = initialStats;
    }

    public virtual void TakeDamage(float damage)
    {
        // ����, ���� ����� ���δ� ���߿� ������ (AttackType�� ���� ����)
        float actualDamage = Mathf.Max(damage - stats.Defense, 0);
        stats.Health -= actualDamage;
        if (stats.Health <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        // ��� ó�� ����
        if (stats.Health <= 0) 
        { 
            Destroy(gameObject);
        }
    }

    // ���� ��, ���� ���ݱ����� ��� �ð��� �����Ѵ�.
    protected virtual IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(1f / stats.AttackSpeed);
        canAttack = true;
    }

    // ���� ���� ���� �Ǻ� �޼���
    public abstract bool CanAttack(Vector3 targetPosition);

    public virtual void Attack(Unit target)
    {
        if (canAttack)
        {
            target.TakeDamage(stats.AttackPower);
            StartCoroutine(AttackCooldown()); 
        }
    }
}
