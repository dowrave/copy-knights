using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Unit : MonoBehaviour
{
    [SerializeField, HideInInspector]
    protected UnitData stats;// 실제 데이터를 저장하는 필드
    public UnitData Stats => stats; // Stats 프로퍼티

    public AttackRangeType attackRangeType; // 자식 컴포넌트들에서 사용

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
        // 물리, 마법 대미지 여부는 나중에 구현함 (AttackType에 따라 구분)

        // 실제 대미지 대비 5%는 무조건 들어가게 구현되었을 거임
        float actualDamage = Mathf.Max(damage - stats.defense, damage * 0.05f);

        stats.health -= actualDamage;

        if (stats.health <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        // 사망 처리 로직
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
