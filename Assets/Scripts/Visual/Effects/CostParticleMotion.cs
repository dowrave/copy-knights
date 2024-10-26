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

    private Vector2 iconScreenPosition;
    private Vector3 iconWorldPosition;

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
        // ��ƼŬ �ý����� ��ǥ�� ���� �������� ó���ǹǷ�, ���� ��ǥ�� ��ȯ
        Vector3 particleWorldPosition = particleSystem.transform.TransformPoint(particle.position);

        float distanceToTarget = Vector3.Distance(particle.position, iconWorldPosition);

        uint particleId = particle.randomSeed;

        // �ʱ� �ӵ� ���� - ������ �������� �ణ ������ ��
        if (!particleVelocities.ContainsKey(particleId))
        {
            Vector3 initialVelocity = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0f, 0.5f),
                Random.Range(-0.3f, 0.3f)
            );
            particleVelocities[particleId] = initialVelocity;
        }

        // ���� ��ǥ������ ���� ���
        Vector3 directionToTarget = (iconWorldPosition - particleWorldPosition).normalized;
        Vector3 targetVelocity = directionToTarget * moveSpeed;

        // ���� ���� �ӵ� -> ���� ���� �ӵ��� ��ȯ : ���߿� ������ particle.velocity�� ���� ��ǥ�迡�� �����ϱ� ������ �� �۾��� ��ģ��
        Vector3 localTargetVelocity = particleSystem.transform.InverseTransformDirection(targetVelocity);

        // �ε巯�� ���� ��ȯ
        Vector3 newVelocity = Vector3.Lerp(
            particleVelocities[particleId],
            localTargetVelocity,
            Time.deltaTime * turnSpeed
        );

        // ��ƼŬ ũ�� ����
        float lifeProgress = (elapsed - initialDuration) / (particle.startLifetime - initialDuration);
        particle.startSize *= Mathf.Lerp(1f, 0.8f, lifeProgress);

        // ���ο� �ӵ� ����
        particleVelocities[particleId] = newVelocity;
        particle.velocity = newVelocity;

        // ��ǥ ���� ��ó ���� �� ��ƼŬ ����
        if (distanceToTarget < arrivalThreshold)
        {
            particle.remainingLifetime = 0f;
        }
    }
}