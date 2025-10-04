using UnityEngine;
using System.Collections.Generic;

public class CostParticleMotionVFXController : SelfReturnVFXController
{
    [Header("Movement Settings")]
    [SerializeField] private float initialDuration = 1f;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private float arrivalThreshold = 2f;

    // private new ParticleSystem particleSystem = default!;
    private ParticleSystem.Particle[] particles = System.Array.Empty<ParticleSystem.Particle>();
    private float elapsed = 0f;

    private Vector3 iconWorldPosition;
    // private bool isReturningToPool;

    private RectTransform deploymentCostIconTransform = default!;
    private Dictionary<uint, Vector3> particleVelocities = new Dictionary<uint, Vector3>();
   

    private void Awake()
    {
        if (ps == null)
        {
            ps = GetComponent<ParticleSystem>();
        }

        particles = new ParticleSystem.Particle[ps.main.maxParticles];

        if (deploymentCostIconTransform == null)
        {
            GameObject DeploymentCostIconObject = GameObject.Find("MainCanvas/DeploymentPanel/DeploymentCostIcon");
            deploymentCostIconTransform = DeploymentCostIconObject.GetComponent<RectTransform>();
        }
    }

    public new void OnObjectSpawn(string tag)
    {
        base.OnObjectSpawn(tag); 
        
        elapsed = 0f;
        // isReturningToPool = false;
        particleVelocities.Clear();
    }

    // �������̵��ؼ� �θ��� �ð� ��� LifeCycle �ڷ�ƾ�� ���� - ���⼱ ��ƼŬ�� ��� ���ŵ� ������ Ǯ�� ���ư����� ��
    public override void Initialize(float duration, UnitEntity caster = null)
    {
        if (StageUIManager.Instance != null)
        {
            iconWorldPosition = StageUIManager.Instance.CostIconWorldPosition;
        }
        else
        {
            Debug.LogError("StageUIManager.Instance is not available!");
            ReturnToPool(); // �ʱ�ȭ ���� �� ��� ��ȯ
            return;
        }

        ps.Play(true);
    }

    private void LateUpdate()
    {
        // if (isReturningToPool) return;

        elapsed += Time.deltaTime;
        int numAliveParticles = ps.GetParticles(particles);

        if (elapsed >= initialDuration)
        {
            for (int i = 0; i < numAliveParticles; i++)
            {
                MoveParticleTowardsTarget(ref particles[i]);
            }
        }

        ps.SetParticles(particles, numAliveParticles);

        // ��� ��ƼŬ�� ������� Ǯ�� ���ư�
        // ���� ������ ��ƼŬ�� �����Ǳ� ���� numAliveParticles == 0�� �Ǵ� ��츦 ����
        if (elapsed > initialDuration && numAliveParticles == 0)
        {
            // isReturningToPool = true;
            ReturnToPool();
        }
    }

    private void MoveParticleTowardsTarget(ref ParticleSystem.Particle particle)
    {
        Vector3 particleWorldPosition = GetComponent<ParticleSystem>().transform.TransformPoint(particle.position);
        Vector3 projectedParticlePosition = new Vector3(particleWorldPosition.x, 0f, particleWorldPosition.z);

        float distanceToTarget = Vector3.Distance(projectedParticlePosition, iconWorldPosition);

        if (distanceToTarget < arrivalThreshold)
        {
            particle.remainingLifetime = 0f;
            // Destroy(GetComponent<ParticleSystem>().gameObject, 0.1f);
            return;
        }

        uint particleId = particle.randomSeed;

        if (!particleVelocities.ContainsKey(particleId))
        {
            Vector3 initialVelocity = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0.5f, 1f),
                Random.Range(-0.3f, 0.3f)
            );
            particleVelocities[particleId] = initialVelocity;
        }

        Vector3 directionToTarget = (iconWorldPosition - particleWorldPosition).normalized;
        Vector3 targetVelocity = directionToTarget * moveSpeed;

        Vector3 localTargetVelocity = GetComponent<ParticleSystem>().transform.InverseTransformDirection(targetVelocity);

        Vector3 newVelocity = Vector3.Lerp(
            particleVelocities[particleId],
            localTargetVelocity,
            Time.deltaTime * turnSpeed
        );

        particleVelocities[particleId] = newVelocity;
        particle.velocity = newVelocity;
    }
}