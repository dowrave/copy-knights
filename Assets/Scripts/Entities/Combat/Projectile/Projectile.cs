using UnityEngine;
using UnityEngine.VFX;
using static ICombatEntity;

// 투사체 관리 클래스
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    private float value; // 대미지 or 힐값
    private bool showValue;
    private AttackType attackType;
    private bool isHealing = false;
    private UnitEntity attacker;
    private UnitEntity target;
    private Vector3 lastKnownPosition; // 마지막으로 알려진 적의 위치
    private string poolTag;
    public string PoolTag { get; private set; }
    private bool shouldDestroy;
    private VisualEffect vfx; 


    public void Initialize(UnitEntity attacker,
        UnitEntity target, 
        float value, 
        bool showValue, 
        string poolTag,
        GameObject hitEffectPrefab)
    {
        UnSubscribeFromEvents();

        this.attacker = attacker;
        this.target = target;
        this.value = value;
        this.showValue = showValue;
        this.poolTag = poolTag;
        lastKnownPosition = target.transform.position;
        shouldDestroy = false;

        target.OnDestroyed += OnTargetDestroyed;
        attacker.OnDestroyed += OnAttackerDestroyed;

        vfx = GetComponent<VisualEffect>();
        if (vfx != null)
        {
            vfx.Play();
        }

        // 공격자와 대상이 같다면 힐로 간주
        if (attacker.Faction == target.Faction)
        {
            isHealing = true;
            this.showValue = true;
        }
    }

    private void Update()
    {
        // 공격자가 사라졌고 아직 감지하지 못한 상태
        if (attacker == null && !shouldDestroy)
        {
            shouldDestroy = true; // 목표 도달 후에 파괴
        }
        // 타겟이 살아 있다면 위치 갱신
        if (target != null)
        {
            lastKnownPosition = target.transform.position;
        }

        // 마지막으로 알려진 위치로 이동
        Vector3 direction = (lastKnownPosition - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // 목표 지점 도달 확인
        if (Vector3.Distance(transform.position, lastKnownPosition) < 0.1f)
        {
            OnReachTarget();
        }
    }

    /// <summary>
    /// 목표 위치에 도달 시에 동작
    /// </summary>
    private void OnReachTarget()
    {
        // 타겟이 살아있는 경우
        if (target != null)
        {
            AttackSource attackSource = new AttackSource(transform.position, true);

            if (isHealing)
            {
                // 힐 이펙트도 피격 이펙트로 포함하겠음
                target.TakeHeal(attacker, attackSource, value);
            }
            else
            {
                // 대미지는 보여야 하는 경우에만 보여줌
                if (showValue == true)
                {
                    ObjectPoolManager.Instance.ShowFloatingText(target.transform.position, value, false);
                }

                target.TakeDamage(attacker, attackSource, value);

            }
        }

        // 공격자가 사라졌거나, 풀이 제거 예정인 경우
        if (shouldDestroy)
        {
            Destroy(gameObject);
        }
        else
        {
            ObjectPoolManager.Instance.ReturnToPool(poolTag, gameObject);
        }
    }

    private void OnTargetDestroyed()
    {
        if (target != null)
        {
            lastKnownPosition = target.transform.position;
            target.OnDestroyed -= OnTargetDestroyed;
            target = null; 
        }
    }

    private void OnAttackerDestroyed()
    {
        if (attacker != null)
        {
            shouldDestroy = true;
            attacker.OnDestroyed -= OnAttackerDestroyed;
            attacker = null;
        }
    }

    private void UnSubscribeFromEvents()
    {
        if (target != null)
        {
            target.OnDestroyed -= OnTargetDestroyed;
        }
        if (attacker != null)
        {
            attacker.OnDestroyed -= OnAttackerDestroyed; 
        }
    }

    // 풀에서 재사용될 때 호출될 메서드
    private void OnDisable()
    {
        UnSubscribeFromEvents();

        if (vfx != null)
        {
            vfx.Stop();
        }

        target = null;
        attacker = null;
        lastKnownPosition = Vector3.zero;
        shouldDestroy = false;
    }

    private void OnDestroy()
    {
        UnSubscribeFromEvents();
    }
}
