using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 투사체 관리 클래스
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float value;
    public bool showValue;
    public AttackType attackType;
    private bool isHealing = false;
    private UnitEntity attacker;
    private UnitEntity target;
    private Vector3 lastKnownPosition; // 마지막으로 알려진 적의 위치
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

        // 공격자와 대상이 같다면 힐로 간주
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

            if (isHealing)
            {
                target.TakeHeal(value, attacker);
            }
            else
            {
                target.TakeDamage(attackType, value, attacker);

                // 대미지는 보여야 하는 경우에만 보여줌
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
