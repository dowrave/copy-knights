using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CostParticleMotion : MonoBehaviour
{
    // 목적지 위치 - MainCanvas의 DeploymentCostIcon 
    [SerializeField] private RectTransform deploymentCostIconTransform;
    private new ParticleSystem particleSystem;
    private ParticleSystem.Particle[] particles;
    private float elapsed = 0f;
    private Vector3 targetPos; // 파티클들의 최종 목적지


    [Header("Movement Settings")]
    [SerializeField] private float initialDuration = 0.5f;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float turnSpeed = 5f; // 방향 전환 속도 조절

    // 각 파티클의 현재 진행 방향을 저장
    private Dictionary<uint, Vector3> particleVelocities = new Dictionary<uint, Vector3>();

    private void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];

        // UI 요소 할당 X 시
        if (deploymentCostIconTransform == null)
        {
            deploymentCostIconTransform = GameObject.Find("MainCanvas/DeploymentCostPanel/DeploymentCostIcon").GetComponent<RectTransform>();
        }

        Vector3 uiWorldPos = GetWorldPositionFromUI(deploymentCostIconTransform);
        targetPos = new Vector3(
            uiWorldPos.x,
            uiWorldPos.y,
            Camera.main.transform.position.z + 10f
        );
        Debug.Log($"targetPos : {targetPos}");
    }

    private void LateUpdate()
    {
        elapsed += Time.deltaTime;
        int numAliveParticles = particleSystem.GetParticles(particles);

        if (elapsed >= initialDuration)
        {
            for (int i = 0; i < numAliveParticles; i++)
            {
                SmoothlyMoveToDestination(ref particles[i]);

                // 시간에 따른 크기 감소
                float sizeMultiplier = Mathf.Lerp(1f, 0.2f, (elapsed - initialDuration) / (particles[i].startLifetime - initialDuration));
                particles[i].startSize = particles[i].startSize * sizeMultiplier;

            }
        }

        particleSystem.SetParticles(particles, numAliveParticles);
    }

    private Vector3 GetWorldPositionFromUI(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners); // 0부터 3까지 좌하->좌상->우상->우하

        // 스크린 좌표로 변환
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, corners[3]);

        // 약간 앞쪽의 월드 좌표로 변환 
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        float distance = 10f; // 카메라로부터의 거리
        Vector3 worldPos = ray.GetPoint(distance);

        return worldPos;
    }

    /// <summary>
    /// 파티클이 올라간 다음, 방향을 서서히 전환하면서 목적지로 향함
    /// </summary>
    private void SmoothlyMoveToDestination(ref ParticleSystem.Particle particle)
    {
        uint particleId = particle.randomSeed;

        // 목표 방향 계산
        Vector3 directionToTarget = (targetPos - particle.position).normalized;
        Vector3 targetVelocity = directionToTarget * moveSpeed;

        // 현재 진행 방향이 없다면 초기화
        if (!particleVelocities.ContainsKey(particleId))
        {
            Vector3 initialVelocity = particle.velocity;
            if (initialVelocity == Vector3.zero)
            {
                // Velocity over Lifetime 설정값을 반영
                initialVelocity = new Vector3(
                    Random.Range(-0.3f, 0.3f),
                    Random.Range(0f, 0.5f),
                    Random.Range(0f, 1f)
                );
            }

            particleVelocities[particleId] = initialVelocity;

            Debug.Log($"{particleVelocities[particleId]}에 {initialVelocity} 할당");
        }

        // 현재 속도와 목표 속도 간의 보간
        Vector3 newVelocity = Vector3.Lerp(
            particleVelocities[particleId],
            targetVelocity,
            Time.deltaTime * turnSpeed
        );

        // 새로운 속도 저장 및 적용
        particleVelocities[particleId] = newVelocity;
        particle.velocity = newVelocity;
    }

    private void OnParticleSystemStopped()
    {
        particleVelocities.Clear();
    }
}
