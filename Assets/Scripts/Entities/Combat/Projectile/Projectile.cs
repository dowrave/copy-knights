using UnityEngine;
using UnityEngine.VFX;
using static ICombatEntity;

// 투사체 관리 클래스
public class Projectile : MonoBehaviour
{
    public float speed = 5f;
    private float value; // 대미지 or 힐값
    private bool showValue;
    private bool isHealing = false;
    private UnitEntity attacker;
    private UnitEntity target;
    private Vector3 lastKnownPosition; // 마지막으로 알려진 적의 위치
    private string poolTag;
    private GameObject hitEffectPrefab;
    public string PoolTag { get; private set; }
    private bool shouldDestroy;

    private VisualEffect vfx;
    private Vector3 vfxBaseDirection;

    // VFX에서 자체 회전을 가질 때에만 사용
    private float rotationSpeed = 360f; // 초당 회전 각도 (도 단위)
    private float currentRotation = 0f;

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

        this.hitEffectPrefab = hitEffectPrefab;
        target.OnDestroyed += OnTargetDestroyed;
        attacker.OnDestroyed += OnAttackerDestroyed;

        vfx = GetComponentInChildren<VisualEffect>();

        if (vfx != null)
        {
            InitializeVFXDirection(); // 방향이 있는 VFX는 초기 방향을 설정함
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

        // 방향 계산 및 이동
        Vector3 direction = (lastKnownPosition - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        UpdateVFXDirection(direction);

        // 목표 지점 도달 확인
        if (Vector3.Distance(transform.position, lastKnownPosition) < 0.1f)
        {
            OnReachTarget();
        }
    }

    // 방향이 있는 VFX의 초기 방향 설정
    private void InitializeVFXDirection() 
    {
        if (vfx != null && vfx.HasVector3("BaseDirection"))
        {
            vfxBaseDirection = vfx.GetVector3("BaseDirection").normalized;

            // 초기 방향 계산
            Vector3 initialDirection = (lastKnownPosition - transform.position).normalized;

            // 기본 -> 목표 방향으로의 회전 계산해서 VFX에 전달
            Quaternion rotation = Quaternion.FromToRotation(vfxBaseDirection, initialDirection);
            Vector3 eulerAngles = rotation.eulerAngles;

            // VFX에 초기 회전 적용
            if (vfx.HasVector3("EulerAngle"))
            {
                vfx.SetVector3("EulerAngle", eulerAngles);
            }
            if (vfx.HasVector3("FlyingDirection"))
            {
                vfx.SetVector3("FlyingDirection", initialDirection);
            }
        }
    }


    // 방향 벡터를 받아 VFX에 오일러 각으로 변환해 전달한다
    private void UpdateVFXDirection(Vector3 directionVector)
    {
        if (vfx != null)
        {
            if (vfx.HasVector3("FlyingDirection"))
            {
                vfx.SetVector3("FlyingDirection", directionVector);
            }

            // 이펙트의 방향에 따른 회전
            if (vfxBaseDirection != null)
            {
                Quaternion directionRotation = Quaternion.FromToRotation(vfxBaseDirection, directionVector);
                Vector3 eulerAngles = directionRotation.eulerAngles;

                // 자체적인 회전을 갖는 이펙트라면
                if (vfx.HasBool("SelfRotation"))
                {
                    currentRotation += rotationSpeed * Time.deltaTime;
                    currentRotation %= 360f; // 360도를 넘지 않는 정규화

                    // 진행 방향을 축으로 하는 자체 회전
                    Quaternion axialRotation = Quaternion.AngleAxis(currentRotation, directionVector);

                    // 회전 결합 (결합 순서가 중요함)
                    Quaternion finalRotation = axialRotation * directionRotation;
                    eulerAngles = finalRotation.eulerAngles;
                }


                if (vfx.HasVector3("EulerAngle"))
                {
                    vfx.SetVector3("EulerAngle", eulerAngles);
                }
            }
        }
    }

    // 목표 위치에 도달 시에 동작
    private void OnReachTarget()
    {
        // 타겟이 살아있는 경우
        if (target != null)
        {
            AttackSource attackSource = new AttackSource(transform.position, true, hitEffectPrefab);

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

    private void OnTargetDestroyed(UnitEntity unit)
    {
        if (target != null)
        {
            lastKnownPosition = target.transform.position;
            target.OnDestroyed -= OnTargetDestroyed;
            target = null; 
        }
    }

    private void OnAttackerDestroyed(UnitEntity unit)
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

    private void OnEnable()
    {
        vfx = GetComponentInChildren<VisualEffect>();
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
