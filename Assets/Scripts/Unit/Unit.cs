using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackType
{
    Physical,
    Magical,
    True
}

public enum AttackRangeType
{
    Melee,
    Ranged
}

public abstract class Unit : MonoBehaviour
{
    [SerializeField, HideInInspector]
    protected UnitStats stats;// 실제 데이터를 저장하는 필드
    public UnitStats Stats => stats; // Stats 프로퍼티
    // 이 => 부분은 이것과 같다.
    /*
     public UnitStats Stats
    {
        get { return stats; }
    }
     */

    protected bool canAttack = true;
    public AttackRangeType attackRangeType;

    protected HealthBar healthBar;

    public virtual void Initialize(UnitStats initialStats)
    {
        stats = initialStats;
        healthBar = GetComponentInChildren<HealthBar>();
    }

    public virtual void TakeDamage(float damage)
    {
        // 물리, 마법 대미지 여부는 나중에 구현함 (AttackType에 따라 구분)

        // 실제 대미지 대비 5%는 무조건 들어가게 구현되었을 거임
        float actualDamage = Mathf.Max(damage - stats.Defense, damage * 0.05f);

        stats.Health -= actualDamage;

        if (stats.Health <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        // 사망 처리 로직
        if (stats.Health <= 0) 
        { 
            Destroy(gameObject);
        }
    }

    // 공격 후, 다음 공격까지의 대기 시간을 관리한다.
    protected virtual IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(1f / stats.AttackSpeed); // 코루틴은 백그라운드에서 실행, 공격 쿨타임 중 다른 행동(이동)을 방해하지 않는다.
        canAttack = true;
    }

    // 공격 가능 여부 판별 메서드
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
