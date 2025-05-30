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
    private UnitEntity? attacker; // 날아가는 중에 파괴될 수 있음
    private UnitEntity? target; // 날아가는 중에 파괴될 수 있음
    private Vector3 lastKnownPosition = new Vector3(0f, 0f, 0f); // 마지막으로 알려진 적의 위치
    private string poolTag = string.Empty;
    private string hitEffectTag = string.Empty;
    private GameObject hitEffectPrefab = default!;
    private bool shouldDestroy = false;

    private VisualEffect? vfx;
    private Vector3 vfxBaseDirection = new Vector3(0f, 0f, 0f);

    // VFX에서 자체 회전을 가질 때에만 사용
    private float rotationSpeed = 360f; // 초당 회전 각도 (도 단위)
    private float currentRotation = 0f;

    public void Initialize(UnitEntity attacker,
        UnitEntity target, 
        float value, 
        bool showValue, 
        string poolTag,
        GameObject hitEffectPrefab, 
        string hitEffectTag)
    {
        // 공격자(attacker)와 대상(target)은 Initialize 메서드의 인자로 반드시 전달되므로 null일 수 없다고 가정할 수 있습니다.
        // 따라서 null 확인 없이 바로 Faction을 비교할 수 있습니다.
        if (attacker.Faction == target.Faction)
        {
            isHealing = true;
            this.showValue = true;
        }
        UnSubscribeFromEvents();

        this.attacker = attacker;
        this.target = target;
        this.value = value;
        this.showValue = showValue;
        this.poolTag = poolTag;
        lastKnownPosition = target.transform.position;
        shouldDestroy = false;

        this.hitEffectPrefab = hitEffectPrefab;

        vfx = GetComponentInChildren<VisualEffect>();

        if (vfx != null)
        {
            InitializeVFXDirection(); // 방향이 있는 VFX는 초기 방향을 설정함
            vfx.Play();
        }

        // 이 시점의 target, attacker은 null이 아님
        target.OnDestroyed += OnTargetDestroyed;
        attacker.OnDestroyed += OnAttackerDestroyed;
     
        // 공격자와 대상이 같다면 힐로 간주
        //if (attacker.Faction == target.Faction)
        //{
        //    isHealing = true;
        //    this.showValue = true;
        //}
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
        if (target != null && attacker != null)
        {
            AttackSource attackSource = new AttackSource(transform.position, true, hitEffectPrefab, hitEffectTag);

            // 힐 상황
            if (isHealing)
            {
                // 힐 이펙트도 피격 이펙트로 포함하겠음
                target.TakeHeal(attacker, attackSource, value);
            }

            // 범위 공격 상황
            else if (attacker is Operator op && op.OperatorData.operatorClass == OperatorData.OperatorClass.Artillery)
            {
                CreateAreaOfDamage(transform.position, value, showValue, attackSource);
            }

            // 단일 공격
            else
            {
                // 대미지는 보여야 하는 경우에만 보여줌
                if (showValue == true)
                {
                    ObjectPoolManager.Instance!.ShowFloatingText(target.transform.position, value, false);
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
            ObjectPoolManager.Instance!.ReturnToPool(poolTag, gameObject);
        }
    }
    
    // 범위 공격을 하는 경우 이펙트와 콜라이더를 생성함
    private void CreateAreaOfDamage(Vector3 position, float damage, bool showValue, AttackSource attackSource)
    {
        // 범위 공격 이펙트 생성
        if (hitEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            Destroy(effectInstance, 2f); // 2초 후에 이펙트 제거
        }

        // 범위 공격 대상에게 대미지 적용
        Collider[] hitColliders = Physics.OverlapSphere(position, 0.5f);
        foreach (var hitCollider in hitColliders)
        {
            UnitEntity unit = hitCollider.GetComponent<UnitEntity>();
            if (unit != null && unit.Faction != attacker.Faction) // 다른 세력일 때에만 대미지를 준다
            {
                if (showValue)
                {
                    ObjectPoolManager.Instance!.ShowFloatingText(unit.transform.position, damage, false);
                }
                
                unit.TakeDamage(attacker, attackSource, damage);
            }
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
