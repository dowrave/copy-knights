using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 투사체 관리 클래스
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage;
    public AttackType attackType;
    private UnitEntity target;
    private Vector3 lastKnownPosition; // 마지막으로 알려진 적의 위치
    private string poolTag;
    public string PoolTag { get; private set; }
    private bool isMarkedForRemoval;

    public void Initialize(UnitEntity target, AttackType attackType, float damage, string poolTag)
    {
        this.target = target;
        this.attackType = attackType;
        this.damage = damage;
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

        // 타겟이 살아 있다면 위치 갱신
        lastKnownPosition = target.transform.position;

        // 마지막으로 알려진 위치로 이동
        Vector3 direction = (lastKnownPosition - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // 목표 지점 도달 확인
        if (Vector3.Distance(transform.position, lastKnownPosition) < 0.1f)
        {
            OnReachTarget();
        }
    }

    private void OnReachTarget()
    {
        if (target != null)
        {
            target.TakeDamage(attackType, damage);
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        ObjectPoolManager.Instance.ReturnToPool(poolTag, gameObject);
    }

    // 풀에서 재사용될 때 호출될 메서드
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
