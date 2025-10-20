using UnityEngine;
using UnityEngine.VFX;
using static ICombatEntity;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// 투사체 관리 클래스
public class Projectile : MonoBehaviour
{
    // 이펙트 할당
    [Header("VFX")]
    [SerializeField] private VisualEffect? vfxGraph;
    [SerializeField] private ParticleSystem? mainParticle;
    [SerializeField] private List<ParticleSystem> remainingParticles; // mainParticle이 사라지더라도 표시되는 파티클

    [Header("Check if VFX needs Rotation")]
    [SerializeField] private bool needToRotate; // 방향이 정해진 파티클의 경우 체크

    [Header("Speed")]
    public float speed = 5f;

    [Header("Wait Time Before VFX Stop")]
    // 파괴되거나 풀로 돌아가기 전 대기 시간 - 이펙트가 바로 사라지지 않게 해서 보기 어색하지 않게끔 함
    [SerializeField] private float WAIT_DISAPPEAR_TIME = 0.5f;

    [Header("Mesh Renderers")]
    [SerializeField] private List<MeshRenderer> renderers;

    private float value; // 대미지 or 힐값
    private bool showValue;
    private bool isHealing = false;
    private UnitEntity? attacker; // 날아가는 중에 파괴될 수 있음
    private UnitEntity? target; // 날아가는 중에 파괴될 수 있음
    private Vector3 lastKnownPosition = new Vector3(0f, 0f, 0f); // 마지막으로 알려진 적의 위치
    private string poolTag = string.Empty;
    private string hitEffectTag = string.Empty;
    private bool shouldDestroy = false;
    private AttackType attackType;

    private Vector3 vfxBaseDirection = new Vector3(0f, 0f, 0f);

    private bool isReachedTarget = false;

    private SphereCollider sphereCollider;



    // VFX에서 자체 회전을 가질 때에만 사용
    private float rotationSpeed = 360f; // 초당 회전 각도 (도 단위)
    private float currentRotation = 0f;

    private void Awake()
    {
        // 둘 다 없을 때 찾아봄
        if (vfxGraph == null && mainParticle == null)
        {
            mainParticle = GetComponentInChildren<ParticleSystem>();
            if (mainParticle == null)
            {
                vfxGraph = GetComponentInChildren<VisualEffect>();
            }
        }

        if (sphereCollider == null)
        {
            sphereCollider = GetComponent<SphereCollider>();
        }
    }

    public void Initialize(UnitEntity attacker,
        UnitEntity target,
        float value,
        bool showValue,
        string poolTag,
        string hitEffectTag,
        AttackType attackType)
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
        this.attackType = attackType;
        lastKnownPosition = target.transform.position;
        shouldDestroy = false;

        this.hitEffectTag = hitEffectTag;

        // 메쉬로 구현된 요소가 있다면 활성화
        if (renderers != null)
        {
            foreach (MeshRenderer renderer in renderers)
            {
                renderer.enabled = true;
            }
        }

        if (needToRotate)
        {
            InitializeVFXDirection(); // 방향이 있는 VFX는 초기 방향을 설정함
        }
        isReachedTarget = false;

        // 이 시점의 target, attacker은 null이 아님
        target.OnDeathAnimationCompleted += OnTargetDestroyed;
        attacker.OnDeathAnimationCompleted += OnAttackerDestroyed;

        // 공격자와 대상이 같다면 힐로 간주
        //if (attacker.Faction == target.Faction)
        //{
        //    isHealing = true;
        //    this.showValue = true;
        //}
    }
    private void Update()
    {

        if (isReachedTarget) return;

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

        // 회전 설정 반영
        if (needToRotate)
        {
            UpdateVFXDirection(direction);
        }

        // 목표 지점 도달 확인
        // if (Vector3.Distance(transform.position, lastKnownPosition) < 0.1f)
        // 이제 타겟이 사라졌을 때에만 실행됨
        if (target == null)
        {
            float reachDistance = sphereCollider.radius;
            if ((transform.position - lastKnownPosition).sqrMagnitude < reachDistance * reachDistance) // 이게 더 가볍단다
            {
                HandleHit(lastKnownPosition);
            }
        }
    }

    // 방향이 있는 VFX의 초기 방향 설정
    private void InitializeVFXDirection()
    {
        if (vfxGraph != null && vfxGraph.HasVector3("BaseDirection"))
        {
            vfxBaseDirection = vfxGraph.GetVector3("BaseDirection").normalized;

            // 초기 방향 계산
            Vector3 initialDirection = (lastKnownPosition - transform.position).normalized;

            // 기본 -> 목표 방향으로의 회전 계산해서 VFX에 전달
            Quaternion rotation = Quaternion.FromToRotation(vfxBaseDirection, initialDirection);
            Vector3 eulerAngles = rotation.eulerAngles;

            // VFX에 초기 회전 적용
            if (vfxGraph.HasVector3("EulerAngle"))
            {
                vfxGraph.SetVector3("EulerAngle", eulerAngles);
            }
            if (vfxGraph.HasVector3("FlyingDirection"))
            {
                vfxGraph.SetVector3("FlyingDirection", initialDirection);
            }

            vfxGraph.Play();
        }
        else
        {
            // Update에 통합되어 있지만 초기화에서도 한 번 잡아주겠음
            Vector3 direction = (lastKnownPosition - transform.position).normalized;
            Quaternion objectRotation = Quaternion.LookRotation(direction); // 이펙트가 +Z축을 향한다고 가정
            transform.rotation = objectRotation;

            mainParticle.Play(true);
        }
    }


    // 방향 벡터를 받아 VFX에 오일러 각으로 변환해 전달한다
    private void UpdateVFXDirection(Vector3 directionVector)
    {
        if (vfxGraph != null)
        {
            if (vfxGraph.HasVector3("FlyingDirection"))
            {
                vfxGraph.SetVector3("FlyingDirection", directionVector);
            }

            // 이펙트의 방향에 따른 회전
            if (vfxBaseDirection != null)
            {
                Quaternion directionRotation = Quaternion.FromToRotation(vfxBaseDirection, directionVector);
                Vector3 eulerAngles = directionRotation.eulerAngles;

                // 자체적인 회전을 갖는 이펙트라면
                if (vfxGraph.HasBool("SelfRotation"))
                {
                    currentRotation += rotationSpeed * Time.deltaTime;
                    currentRotation %= 360f; // 360도를 넘지 않는 정규화

                    // 진행 방향을 축으로 하는 자체 회전
                    Quaternion axialRotation = Quaternion.AngleAxis(currentRotation, directionVector);

                    // 회전 결합 (결합 순서가 중요함)
                    Quaternion finalRotation = axialRotation * directionRotation;
                    eulerAngles = finalRotation.eulerAngles;
                }


                if (vfxGraph.HasVector3("EulerAngle"))
                {
                    vfxGraph.SetVector3("EulerAngle", eulerAngles);
                }
            }
        }
        else if (mainParticle != null)
        {
            Vector3 direction = (lastKnownPosition - transform.position).normalized;
            Quaternion objectRotation = Quaternion.LookRotation(direction); // 테스트
            transform.rotation = objectRotation;
        }
    }

    // 목표 위치에 도달 시에 동작
    private void HandleHit(Vector3 hitPosition)
    {
        if (isReachedTarget) return;
        isReachedTarget = true;

        // 타겟이 살아있는 경우
        if (target != null && attacker != null)
        {
            AttackSource attackSource = new AttackSource(
                attacker: attacker,
                // position: transform.position,
                position: hitPosition,
                damage: value,
                type: attackType,
                isProjectile: true,
                hitEffectTag: hitEffectTag,
                showDamagePopup: showValue
            );


            if (isHealing)
            {
                // 힐 상황
                target.TakeHeal(attackSource);
            }
            else if (attacker is Operator op && op.OperatorData.operatorClass == OperatorData.OperatorClass.Artillery)
            {
                // 범위 공격 상황
                CreateAreaOfDamage(transform.position, value, showValue, attackSource);
            }
            else
            {
                // 단일 공격
                // 대미지는 보여야 하는 경우에만 보여줌
                if (showValue == true)
                {
                    ObjectPoolManager.Instance!.ShowFloatingText(target.transform.position, value, false);
                }
                target.TakeDamage(attackSource);
            }
        }

        // 공격자가 사라졌거나, 풀이 제거 예정인 경우
        if (shouldDestroy)
        {
            Destroy(gameObject, WAIT_DISAPPEAR_TIME);
        }
        else
        {
            StartCoroutine(ReturnToPoolAfterSeconds(WAIT_DISAPPEAR_TIME));
        }
    }

    private IEnumerator ReturnToPoolAfterSeconds(float seconds)
    {

        if (vfxGraph != null)
        {
            vfxGraph.Reinit();
        }
        else if (mainParticle != null)
        {
            // 부모-자식 관계를 일시적으로 끊어서 부모 파티클의 실행을 멈춰도 자식 파티클은 계속 재생되게 함
            foreach (var ps in remainingParticles)
            {
                ps.transform.parent = null;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting); // 파티클의 추가 생성을 막음
                // ps.Play() // 굳이 필요 없어서 주석 처리해봄
            }

            // 남기지 않아도 되는 파티클들 모두 제거
            mainParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // 메쉬 렌더러로 구현된 요소들이 있다면 이들을 모두 비활성화
            if (renderers != null)
            {
                foreach (MeshRenderer renderer in renderers)
                {
                    renderer.enabled = false;
                }
            }
        }

        yield return new WaitForSeconds(seconds);

        // 다시 부모 파티클에 할당
        foreach (var ps in remainingParticles)
        {
            ps.transform.parent = mainParticle.transform;
        }

        ObjectPoolManager.Instance!.ReturnToPool(poolTag, gameObject);
    }

    // 범위 공격을 하는 경우 이펙트와 콜라이더를 생성함
    private void CreateAreaOfDamage(Vector3 position, float damage, bool showValue, AttackSource attackSource)
    {
        // 범위 공격 이펙트 생성
        if (hitEffectTag != null)
        {
            GameObject effectInstance = ObjectPoolManager.Instance.SpawnFromPool(hitEffectTag, position, Quaternion.identity);
            // GameObject effectInstance = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            // Destroy(effectInstance, 2f); // 2초 후에 이펙트 제거. 아래의 코드는 이걸 기다리지 않고 잘 동작함.
        }

        // 범위 공격 대상에게 대미지 적용
        Collider[] hitColliders = Physics.OverlapSphere(position, 0.5f);

        foreach (var hitCollider in hitColliders)
        {
            BodyColliderController targetCollider = hitCollider.GetComponent<BodyColliderController>();
            if (targetCollider != null)
            {
                UnitEntity target = targetCollider.GetComponentInParent<UnitEntity>();
                if (target.Faction != Faction.Neutral &&
                    target.Faction != attacker!.Faction)
                {
                    if (showValue)
                    {
                        ObjectPoolManager.Instance!.ShowFloatingText(target.transform.position, damage, false);
                    }

                    // 피격 이펙트는 적용하지 않음
                    target.TakeDamage(source: attackSource);
                }
            }
        }
    }

    private void OnTargetDestroyed(UnitEntity unit)
    {
        if (target != null)
        {
            lastKnownPosition = target.transform.position;
            target.OnDeathAnimationCompleted -= OnTargetDestroyed;
            target = null;
        }
    }

    private void OnAttackerDestroyed(UnitEntity unit)
    {
        if (attacker != null)
        {
            shouldDestroy = true;
            attacker.OnDeathAnimationCompleted -= OnAttackerDestroyed;
            attacker = null;
        }
    }

    private void UnSubscribeFromEvents()
    {
        if (target != null)
        {
            target.OnDeathAnimationCompleted -= OnTargetDestroyed;
        }
        if (attacker != null)
        {
            attacker.OnDeathAnimationCompleted -= OnAttackerDestroyed;
        }
    }

    private void OnEnable()
    {
        vfxGraph = GetComponentInChildren<VisualEffect>();
    }

    // 풀에서 재사용될 때 호출될 메서드
    private void OnDisable()
    {
        UnSubscribeFromEvents();

        if (vfxGraph != null)
        {
            vfxGraph.Stop();
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
    
    private void OnTriggerEnter(Collider other)
    {
        // 이미 목표에 도달했거나, 타겟이 없으면 무시
        if (isReachedTarget || target == null) return;

        // 충돌한 오브젝트가 내 타겟인지 확인
        BodyColliderController hitUnitCollider = other.GetComponent<BodyColliderController>();
        
        if (hitUnitCollider != null && // 다른 콜라이더는 거름
            hitUnitCollider.ParentUnit == target)
        {
            // OnReachTarget() 대신 새로운 공용 함수 호출
            HandleHit(target.transform.position);
        }
        
    }
}
