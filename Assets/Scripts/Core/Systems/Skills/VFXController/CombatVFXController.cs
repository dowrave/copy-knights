using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

// 풀에서 꺼낸 전투 VFX의 실행과 오브젝트 풀링 관리
public class CombatVFXController : MonoBehaviour
{
    private string _tag;

    private AttackSource attackSource;
    private UnitEntity? target; // null일 수 있음
    private Vector3 targetPosition;
    private float effectDuration;

    [Header("Assign One")]
    [SerializeField] private ParticleSystem? mainPs; 
    [SerializeField] private VisualEffect? vfx;

    [Header("Particle System Options")]
    [SerializeField] private VFXRotationType rotationType = VFXRotationType.None;
    [Tooltip("Billboard의 회전이 반영되어야 하는 파티클 시스템들을 여기에 할당합니다.")]
    // 추가 설명 : 텍스쳐에 방향성이 있는 경우에만 쓰면 됩니다.
    [SerializeField] private List<ParticleSystem> billboardParticles = new List<ParticleSystem>();

    private void Awake()
    {
        vfx = GetComponent<VisualEffect>(); // ps로 구현 시 직접 할당
    }

    // 타겟이 있을 때 - 위치 정보만 뽑아낸다.
    public void Initialize(AttackSource attackSource, UnitEntity target, string tag, float effectDuration = 1f)
    {
        Initialize(attackSource, target.transform.position, tag, effectDuration, target);
    }

    public void Initialize(AttackSource attackSource, Vector3 targetPosition, string tag, float effectDuration = 1f, UnitEntity? target = null)
    {
        _tag = tag;
        this.attackSource = attackSource;
        this.targetPosition = targetPosition;
        this.target = target;
        this.effectDuration = effectDuration;

        if (vfx != null)
        {
            vfx.Stop();
            vfx.Reinit();
            PlayVFXGraph();
        }
        else if (mainPs != null)
        {
            mainPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            PlayPS();
        }

        StartCoroutine(WaitAndReturnToPool(this.effectDuration));
    }

    // 그래프마다 활성화된 프로퍼티가 다름 / 이펙트 사용처가 더 많아지면 더 확장해야 함
    private void PlayVFXGraph()
    {
        if (vfx != null)
        {
            // GetHit에서는 AttackDirection, LifeTime을 사용
            if (vfx.HasVector3("AttackDirection"))
            {
                Vector3 attackDirection = (transform.position - attackSource.Position).normalized;
                vfx.SetVector3("AttackDirection", attackDirection);
            }
            if (vfx.HasFloat("LifeTime"))
            {
                int lifeTimeID = Shader.PropertyToID("Lifetime");
                effectDuration = vfx.GetFloat(lifeTimeID);
            }

            // Attack에선 BaseDirection을 이용
            if (vfx.HasVector3("BaseDirection"))
            {
                Vector3 baseDirection = vfx.GetVector3("BaseDirection");
                Vector3 attackDirection = (targetPosition - transform.position).normalized;
                Quaternion rotation = Quaternion.FromToRotation(baseDirection, attackDirection);
                gameObject.transform.rotation = rotation;
            }

            vfx.Play();
        }
    }

    private void PlayPS()
    {
        if (mainPs != null)
        {
            Vector3 baseDirection = Vector3.forward; // 모든 이펙트는 +Z축으로 진행된다고 가정함

            // 이펙트 오브젝트 자체의 회전 설정
            SetVFXObjectRotation(baseDirection);

            mainPs.Play(true); // true 시 모든 자식 이펙트까지 한꺼번에 재생함
        }
    }

    private void SetVFXObjectRotation(Vector3 baseDirection)
    {
        Vector3 direction = Vector3.zero;

        switch (rotationType)
        {
            // 옵션 1) 피격자 -> 공격자 방향의 이펙트 진행
            case VFXRotationType.targetToSource:

                direction = (attackSource.Position - targetPosition).normalized;
                break;

            // 옵션 2) 공격자 -> 피격자 방향의 이펙트 진행
            case VFXRotationType.sourceToTarget:
                direction = (targetPosition - attackSource.Position).normalized;
                break;

            // 옵션 3) 별도 설정 필요 없음
            case VFXRotationType.None:
                return;
        }

        if (direction != Vector3.zero)
        {
            // 파티클 시스템 오브젝트의 회전
            // Quaternion objectRotation = Quaternion.FromToRotation(baseDirection, direction);
            Quaternion objectRotation = Quaternion.LookRotation(direction); // 테스트
            transform.rotation = objectRotation;

            // 빌보드 파티클의 회전
            // 오브젝트의 Y축 회전값을 라디안으로 변환, startRotationZ값을 업데이트한다. 
            float billboardRotationInRadians = objectRotation.eulerAngles.y * Mathf.Deg2Rad;

            // 캐싱된 모든 모듈의 startRotation에 반영
            for (int i = 0; i < billboardParticles.Count; i++)
            {
                if (billboardParticles[i] != null)
                {
                    var ps = billboardParticles[i];
                    var mainModule = ps.main;
                    mainModule.startRotationZ = new ParticleSystem.MinMaxCurve(billboardRotationInRadians);
                }
            }
        }
    }

    private IEnumerator WaitAndReturnToPool(float duration = 1f)
    {
        yield return new WaitForSeconds(duration);

        if (gameObject != null)
        {
            ObjectPoolManager.Instance!.ReturnToPool(_tag, gameObject);
        }
        
        // else문은 필요 없음 - null이면 Destroy 호출 불가능
    }
}


// 이펙트의 방향 설정
// 일반적으로 부모 오브젝트의 방향을 따르기 때문에 필요 없는 경우가 대부분일 거임! 
// 일단 피격 이펙트에서의 파티클 방향을 설정하기 위해 구현했음
// +Z 방향으로 이펙트가 진행된다고 가정했을 때
// targetToSource : 피격자 -> 공격자 방향으로 이펙트가 진행됨
// sourceToTarget : 공격자 -> 피격자 방향으로 이펙트가 진행됨
public enum VFXRotationType
{
    None,
    targetToSource,
    sourceToTarget,
}