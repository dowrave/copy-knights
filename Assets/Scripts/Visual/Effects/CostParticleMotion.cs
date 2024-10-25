using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 코스트 획득 시 파티클이 코스트 아이콘으로 날아가는 처리
/// World Space의 파티클이 Screen Space - Overlay Canvas의 UI 요소로 이동
/// </summary>
public class CostParticleMotion : MonoBehaviour
{
    [SerializeField] private RectTransform deploymentCostIconTransform;
    private new ParticleSystem particleSystem;
    private ParticleSystem.Particle[] particles;
    private float elapsed = 0f;
    private Vector2 targetScreenPos;

    [Header("Movement Settings")]
    [SerializeField] private float initialDuration = 1f;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private float arrivalThreshold = 0.5f;

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugMarker = true;
    [SerializeField] private float markerSize = 20f;
    [SerializeField] private Color targetMarkerColor = Color.yellow;
    [SerializeField] private Color particleMarkerColor = Color.green;

    private Dictionary<uint, Vector3> particleVelocities = new Dictionary<uint, Vector3>();
   

    private void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];

        if (deploymentCostIconTransform == null)
        {
            deploymentCostIconTransform = GameObject.Find("MainCanvas/DeploymentCostPanel/DeploymentCostIcon").GetComponent<RectTransform>();
        }

        UpdateTargetPosition();
    }

    private void UpdateTargetPosition()
    {
        // 게임 화면(스크린) 상에서의 위치를 잡음
        targetScreenPos = UIManager.Instance.GetUIElementScreenPosition(deploymentCostIconTransform);

        // 월드 투영 좌표 계산 (디버그용)
        Vector3 worldProjection = UIManager.Instance.GetUIElementWorldProjection(deploymentCostIconTransform);
        Debug.Log($"Target Screen Pos: {targetScreenPos}");
        Debug.Log($"World Projection: {worldProjection}");

        // Scene 뷰에서 시각화
        //Debug.DrawSphere(worldProjection, 0.5f, Color.yellow, 1f);
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
        // 파티클의 월드 위치를 스크린 좌표로 변환
        Vector2 particleScreenPos = Camera.main.WorldToScreenPoint(particle.position);

        // 스크린 좌표상에서의 방향 계산
        Vector2 directionToTarget = (targetScreenPos - particleScreenPos).normalized;

        // Screen Space에서의 이동을 World Space로 변환
        Vector3 worldSpaceDirection = Camera.main.ScreenToWorldPoint(new Vector3(
            particleScreenPos.x + directionToTarget.x,
            particleScreenPos.y + directionToTarget.y,
            Camera.main.WorldToScreenPoint(particle.position).z
        )) - particle.position;
        worldSpaceDirection.Normalize();

        uint particleId = particle.randomSeed;
        Vector3 targetVelocity = worldSpaceDirection * moveSpeed;

        // 초기 속도 설정
        if (!particleVelocities.ContainsKey(particleId))
        {
            Vector3 initialVelocity = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0f, 0.5f),
                Random.Range(-0.3f, 0.3f)
            );
            particleVelocities[particleId] = initialVelocity;
        }

        // 부드러운 방향 전환
        Vector3 newVelocity = Vector3.Lerp(
            particleVelocities[particleId],
            targetVelocity,
            Time.deltaTime * turnSpeed
        );

        // 파티클 크기 감소
        float lifeProgress = (elapsed - initialDuration) / (particle.startLifetime - initialDuration);
        particle.startSize *= Mathf.Lerp(1f, 0.2f, lifeProgress);

        // 새로운 속도 적용
        particleVelocities[particleId] = newVelocity;
        particle.velocity = newVelocity;

        // 목표 지점 근처 도달 시 파티클 제거
        if (Vector2.Distance(particleScreenPos, targetScreenPos) < arrivalThreshold)
        {
            particle.remainingLifetime = 0f;
        }
    }

    // 디버깅용
    private void OnGUI()
    {
        if (!showDebugMarker) return;

        // 타겟 위치 표시 (Screen -> GUI 좌표계 변환)
        Vector2 targetGuiPos = new Vector2(targetScreenPos.x, Screen.height - targetScreenPos.y);
        DrawDebugMarker(targetGuiPos, targetMarkerColor, "Target");

        // 활성 파티클 위치 표시
        if (particles != null && particleSystem != null)
        {
            int numAliveParticles = particleSystem.GetParticles(particles);
            for (int i = 0; i < numAliveParticles; i++)
            {
                Vector2 particleScreenPos = Camera.main.WorldToScreenPoint(particles[i].position);
                Vector2 particleGuiPos = new Vector2(particleScreenPos.x, Screen.height - particleScreenPos.y);
                DrawDebugMarker(particleGuiPos, particleMarkerColor, $"P{i}");
            }
        }

        // 화면 해상도와 좌표계 정보
        GUI.Label(
            new Rect(10, 10, 300, 60),
            $"Screen: {Screen.width} x {Screen.height}\n" +
            $"Target Screen Pos: {targetScreenPos}\n" +
            $"Target GUI Pos: {targetGuiPos}"
        );
    }

    private void DrawDebugMarker(Vector2 position, Color color, string label)
    {
        Color originalColor = GUI.color;
        GUI.color = color;

        float halfSize = markerSize * 0.5f;

        // 가로선
        GUI.DrawTexture(
            new Rect(position.x - halfSize, position.y - 1, markerSize, 2),
            Texture2D.whiteTexture
        );

        // 세로선
        GUI.DrawTexture(
            new Rect(position.x - 1, position.y - halfSize, 2, markerSize),
            Texture2D.whiteTexture
        );

        // 좌표 텍스트
        GUI.Label(
            new Rect(position.x + halfSize, position.y + halfSize, 200, 20),
            $"{label}: ({position.x:F0}, {position.y:F0})"
        );

        GUI.color = originalColor;
    }

    private void OnParticleSystemStopped()
    {
        particleVelocities.Clear();
    }
}