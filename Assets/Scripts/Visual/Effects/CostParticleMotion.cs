using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// �ڽ�Ʈ ȹ�� �� ��ƼŬ�� �ڽ�Ʈ ���������� ���ư��� ó��
/// World Space�� ��ƼŬ�� Screen Space - Overlay Canvas�� UI ��ҷ� �̵�
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
        // ���� ȭ��(��ũ��) �󿡼��� ��ġ�� ����
        targetScreenPos = UIManager.Instance.GetUIElementScreenPosition(deploymentCostIconTransform);

        // ���� ���� ��ǥ ��� (����׿�)
        Vector3 worldProjection = UIManager.Instance.GetUIElementWorldProjection(deploymentCostIconTransform);
        Debug.Log($"Target Screen Pos: {targetScreenPos}");
        Debug.Log($"World Projection: {worldProjection}");

        // Scene �信�� �ð�ȭ
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
        // ��ƼŬ�� ���� ��ġ�� ��ũ�� ��ǥ�� ��ȯ
        Vector2 particleScreenPos = Camera.main.WorldToScreenPoint(particle.position);

        // ��ũ�� ��ǥ�󿡼��� ���� ���
        Vector2 directionToTarget = (targetScreenPos - particleScreenPos).normalized;

        // Screen Space������ �̵��� World Space�� ��ȯ
        Vector3 worldSpaceDirection = Camera.main.ScreenToWorldPoint(new Vector3(
            particleScreenPos.x + directionToTarget.x,
            particleScreenPos.y + directionToTarget.y,
            Camera.main.WorldToScreenPoint(particle.position).z
        )) - particle.position;
        worldSpaceDirection.Normalize();

        uint particleId = particle.randomSeed;
        Vector3 targetVelocity = worldSpaceDirection * moveSpeed;

        // �ʱ� �ӵ� ����
        if (!particleVelocities.ContainsKey(particleId))
        {
            Vector3 initialVelocity = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0f, 0.5f),
                Random.Range(-0.3f, 0.3f)
            );
            particleVelocities[particleId] = initialVelocity;
        }

        // �ε巯�� ���� ��ȯ
        Vector3 newVelocity = Vector3.Lerp(
            particleVelocities[particleId],
            targetVelocity,
            Time.deltaTime * turnSpeed
        );

        // ��ƼŬ ũ�� ����
        float lifeProgress = (elapsed - initialDuration) / (particle.startLifetime - initialDuration);
        particle.startSize *= Mathf.Lerp(1f, 0.2f, lifeProgress);

        // ���ο� �ӵ� ����
        particleVelocities[particleId] = newVelocity;
        particle.velocity = newVelocity;

        // ��ǥ ���� ��ó ���� �� ��ƼŬ ����
        if (Vector2.Distance(particleScreenPos, targetScreenPos) < arrivalThreshold)
        {
            particle.remainingLifetime = 0f;
        }
    }

    // ������
    private void OnGUI()
    {
        if (!showDebugMarker) return;

        // Ÿ�� ��ġ ǥ�� (Screen -> GUI ��ǥ�� ��ȯ)
        Vector2 targetGuiPos = new Vector2(targetScreenPos.x, Screen.height - targetScreenPos.y);
        DrawDebugMarker(targetGuiPos, targetMarkerColor, "Target");

        // Ȱ�� ��ƼŬ ��ġ ǥ��
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

        // ȭ�� �ػ󵵿� ��ǥ�� ����
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

        // ���μ�
        GUI.DrawTexture(
            new Rect(position.x - halfSize, position.y - 1, markerSize, 2),
            Texture2D.whiteTexture
        );

        // ���μ�
        GUI.DrawTexture(
            new Rect(position.x - 1, position.y - halfSize, 2, markerSize),
            Texture2D.whiteTexture
        );

        // ��ǥ �ؽ�Ʈ
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