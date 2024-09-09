using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Unit : MonoBehaviour
{
    [SerializeField, HideInInspector]
    protected UnitData stats;// ���� �����͸� �����ϴ� �ʵ�
    public UnitData Stats => stats; // Stats ������Ƽ

    public AttackRangeType attackRangeType; // �ڽ� ������Ʈ�鿡�� ���

    protected HealthBar healthBar;

    protected float attackCooldownTimer = 0f;
    public bool IsAttackCooldownComplete => attackCooldownTimer <= 0f;

    protected virtual void Update()
    {
        if (attackCooldownTimer > 0 )
        {
            attackCooldownTimer -= Time.deltaTime;
        }
    }

    public virtual void Initialize(UnitData initialStats)
    {
        stats = initialStats;
        healthBar = GetComponentInChildren<HealthBar>();
    }

    public virtual void TakeDamage(float damage)
    {
        // ����, ���� ����� ���δ� ���߿� ������ (AttackType�� ���� ����)

        // ���� ����� ��� 5%�� ������ ���� �����Ǿ��� ����
        float actualDamage = Mathf.Max(damage - stats.defense, damage * 0.05f);

        stats.health -= actualDamage;

        if (stats.health <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        // ��� ó�� ����
        if (stats.health <= 0) 
        { 
            Destroy(gameObject);
        }
    }

    protected void StartAttackCooldown()
    {
        attackCooldownTimer = 1f / stats.attackSpeed; 
    }

    public virtual void Attack(Unit target)
    {
        if (IsAttackCooldownComplete)
        {
            PerformAttack(target);
            StartAttackCooldown();
        }
    }

    protected abstract void PerformAttack(Unit target);
    protected abstract bool IsTargetInRange(Unit target);

}
