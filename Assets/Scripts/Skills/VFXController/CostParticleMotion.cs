using UnityEngine;
using System.Collections.Generic;

// �ڽ�Ʈ ȹ�� �� ��ƼŬ�� �ڽ�Ʈ ���������� ���ư��� ó��
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
        // ��ƼŬ �ý����� ��ǥ�� ���� �������� ó���ǹǷ�, ���� ��ǥ�� ��ȯ
        Vector3 particleWorldPosition = particleSystem.transform.TransformPoint(particle.position);
        Vector3 projectedParticlePosition = new Vector3(particleWorldPosition.x, 0f, particleWorldPosition.z);

        float distanceToTarget = Vector3.Distance(projectedParticlePosition, iconWorldPosition);

        // ��ǥ ���� ��ó ���� �� ��ƼŬ ����
        if (distanceToTarget < arrivalThreshold)
        {
            //particle.remainingLifetime = 0f; // �̰ɷ� ���� ��ƼŬ�� �ٽ� ����
            Destroy(particleSystem.gameObject, 0.1f);
            return;
        }

        uint particleId = particle.randomSeed;

        // �ʱ� �ӵ� ���� - ������ �������� �ణ ������ ��
        if (!particleVelocities.ContainsKey(particleId))
        {
            Vector3 initialVelocity = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0.5f, 1f),
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

        // ���ο� �ӵ� ����
        particleVelocities[particleId] = newVelocity;
        particle.velocity = newVelocity;
    }
}