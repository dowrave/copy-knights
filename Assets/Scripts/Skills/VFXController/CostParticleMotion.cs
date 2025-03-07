using UnityEngine;
using System.Collections.Generic;

// 코스트 획득 시 파티클이 코스트 아이콘으로 날아가는 처리
public class CostParticleMotion : MonoBehaviour
{
    private new ParticleSystem particleSystem;
    private ParticleSystem.Particle[] particles;
    private float elapsed = 0f;

    private Vector2 iconScreenPosition;
    private Vector3 iconWorldPosition;

    [Header("Movement Settings")]
    [SerializeField] private float initialDuration = 1f;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private float arrivalThreshold = 2f;

    private RectTransform deploymentCostIconTransform;
    private Dictionary<uint, Vector3> particleVelocities = new Dictionary<uint, Vector3>();
   

    private void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];

        if (deploymentCostIconTransform == null)
        {
            GameObject DeploymentCostIconObject = GameObject.Find("MainCanvas/DeploymentPanel/DeploymentCostIcon");
            deploymentCostIconTransform = DeploymentCostIconObject.GetComponent<RectTransform>();
        }
    }

    private void Start()
    {
        iconWorldPosition = UIManager.Instance.CostIconWorldPosition;
    }

    private void LateUpdate()
    {
        elapsed += Time.deltaTime;
        int numAliveParticles = particleSystem.GetParticles(particles);

        if (elapsed >= initialDuration)
        {
            for (int i = 0; i < numAliveParticles; i++)
            {
                MoveParticleTowardsTarget(ref particles[i]);
            }
        }

        particleSystem.SetParticles(particles, numAliveParticles);
    }

    private void MoveParticleTowardsTarget(ref ParticleSystem.Particle particle)
    {
        // 파티클 시스템의 좌표는 로컬 기준으로 처리되므로, 월드 좌표로 변환
        Vector3 particleWorldPosition = particleSystem.transform.TransformPoint(particle.position);
        Vector3 projectedParticlePosition = new Vector3(particleWorldPosition.x, 0f, particleWorldPosition.z);

        float distanceToTarget = Vector3.Distance(projectedParticlePosition, iconWorldPosition);

        // 목표 지점 근처 도달 시 파티클 제거
        if (distanceToTarget < arrivalThreshold)
        {
            //particle.remainingLifetime = 0f; // 이걸로 쓰면 파티클이 다시 생김
            Destroy(particleSystem.gameObject, 0.1f);
            return;
        }

        uint particleId = particle.randomSeed;

        // 초기 속도 설정 - 랜덤한 방향으로 약간 퍼지게 함
        if (!particleVelocities.ContainsKey(particleId))
        {
            Vector3 initialVelocity = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0.5f, 1f),
                Random.Range(-0.3f, 0.3f)
            );
            particleVelocities[particleId] = initialVelocity;
        }

        // 월드 좌표에서의 방향 계산
        Vector3 directionToTarget = (iconWorldPosition - particleWorldPosition).normalized;
        Vector3 targetVelocity = directionToTarget * moveSpeed;

        // 월드 공간 속도 -> 로컬 공간 속도로 변환 : 나중에 적용할 particle.velocity는 로컬 좌표계에서 동작하기 때문에 이 작업을 거친다
        Vector3 localTargetVelocity = particleSystem.transform.InverseTransformDirection(targetVelocity);

        // 부드러운 방향 전환
        Vector3 newVelocity = Vector3.Lerp(
            particleVelocities[particleId],
            localTargetVelocity,
            Time.deltaTime * turnSpeed
        );

        // 새로운 속도 적용
        particleVelocities[particleId] = newVelocity;
        particle.velocity = newVelocity;
    }
}