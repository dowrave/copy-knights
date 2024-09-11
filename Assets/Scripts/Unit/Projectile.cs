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
            // 타겟이 살아 있다면 위치 갱신
            lastKnownPosition = target.transform.position;
        }

        // 마지막으로 알려진 위치로 이동
        Vector3 direction = (lastKnownPosition - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // 목표 지점 도달 확인
        if (Vector3.Distance(transform.position, lastKnownPosition) < 0.1f)
        {
            if (target != null)
            {
                // 타겟이 살아 있다면 대미지를 입힘
                target.TakeDamage(attackType, damage);
            }
            Destroy(gameObject);
        }
    }

}
