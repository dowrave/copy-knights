using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CostParticleMotion : MonoBehaviour
{
    // ������ ��ġ - MainCanvas�� DeploymentCostIcon 
    [SerializeField] private RectTransform deploymentCostIconTransform;
    private new ParticleSystem particleSystem;
    private ParticleSystem.Particle[] particles;
    private float elapsed = 0f;
    private Vector3 targetPos; // ��ƼŬ���� ���� ������


    [Header("Movement Settings")]
    [SerializeField] private float initialDuration = 0.5f;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float turnSpeed = 5f; // ���� ��ȯ �ӵ� ����

    // �� ��ƼŬ�� ���� ���� ������ ����
    private Dictionary<uint, Vector3> particleVelocities = new Dictionary<uint, Vector3>();

    private void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];

        // UI ��� �Ҵ� X ��
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

                // �ð��� ���� ũ�� ����
                float sizeMultiplier = Mathf.Lerp(1f, 0.2f, (elapsed - initialDuration) / (particles[i].startLifetime - initialDuration));
                particles[i].startSize = particles[i].startSize * sizeMultiplier;

            }
        }

        particleSystem.SetParticles(particles, numAliveParticles);
    }

    private Vector3 GetWorldPositionFromUI(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners); // 0���� 3���� ����->�»�->���->����

        // ��ũ�� ��ǥ�� ��ȯ
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, corners[3]);

        // �ణ ������ ���� ��ǥ�� ��ȯ 
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        float distance = 10f; // ī�޶�κ����� �Ÿ�
        Vector3 worldPos = ray.GetPoint(distance);

        return worldPos;
    }

    /// <summary>
    /// ��ƼŬ�� �ö� ����, ������ ������ ��ȯ�ϸ鼭 �������� ����
    /// </summary>
    private void SmoothlyMoveToDestination(ref ParticleSystem.Particle particle)
    {
        uint particleId = particle.randomSeed;

        // ��ǥ ���� ���
        Vector3 directionToTarget = (targetPos - particle.position).normalized;
        Vector3 targetVelocity = directionToTarget * moveSpeed;

        // ���� ���� ������ ���ٸ� �ʱ�ȭ
        if (!particleVelocities.ContainsKey(particleId))
        {
            Vector3 initialVelocity = particle.velocity;
            if (initialVelocity == Vector3.zero)
            {
                // Velocity over Lifetime �������� �ݿ�
                initialVelocity = new Vector3(
                    Random.Range(-0.3f, 0.3f),
                    Random.Range(0f, 0.5f),
                    Random.Range(0f, 1f)
                );
            }

            particleVelocities[particleId] = initialVelocity;

            Debug.Log($"{particleVelocities[particleId]}�� {initialVelocity} �Ҵ�");
        }

        // ���� �ӵ��� ��ǥ �ӵ� ���� ����
        Vector3 newVelocity = Vector3.Lerp(
            particleVelocities[particleId],
            targetVelocity,
            Time.deltaTime * turnSpeed
        );

        // ���ο� �ӵ� ���� �� ����
        particleVelocities[particleId] = newVelocity;
        particle.velocity = newVelocity;
    }

    private void OnParticleSystemStopped()
    {
        particleVelocities.Clear();
    }
}
