using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

// Ǯ���� ���� ���� VFX�� ����� ������Ʈ Ǯ�� ����
public class CombatVFXController : MonoBehaviour
{
    private string _tag;

    private AttackSource attackSource;
    private UnitEntity? target; // null�� �� ����
    private Vector3 targetPosition;
    private float effectDuration;

    [Header("Assign One")]
    [SerializeField] private ParticleSystem? mainPs; 
    [SerializeField] private VisualEffect? vfx;

    [Header("Particle System Options")]
    [SerializeField] private VFXRotationType rotationType = VFXRotationType.None;
    [Tooltip("Billboard�� ȸ���� �ݿ��Ǿ�� �ϴ� ��ƼŬ �ý��۵��� ���⿡ �Ҵ��մϴ�.")]
    // �߰� ���� : �ؽ��Ŀ� ���⼺�� �ִ� ��쿡�� ���� �˴ϴ�.
    [SerializeField] private List<ParticleSystem> billboardParticles = new List<ParticleSystem>();

    private void Awake()
    {
        vfx = GetComponent<VisualEffect>(); // ps�� ���� �� ���� �Ҵ�
    }

    // Ÿ���� ���� �� - ��ġ ������ �̾Ƴ���.
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

    // �׷������� Ȱ��ȭ�� ������Ƽ�� �ٸ� / ����Ʈ ���ó�� �� �������� �� Ȯ���ؾ� ��
    private void PlayVFXGraph()
    {
        if (vfx != null)
        {
            // GetHit������ AttackDirection, LifeTime�� ���
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

            // Attack���� BaseDirection�� �̿�
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
            Vector3 baseDirection = Vector3.forward; // ��� ����Ʈ�� +Z������ ����ȴٰ� ������

            // ����Ʈ ������Ʈ ��ü�� ȸ�� ����
            SetVFXObjectRotation(baseDirection);

            mainPs.Play(true); // true �� ��� �ڽ� ����Ʈ���� �Ѳ����� �����
        }
    }

    private void SetVFXObjectRotation(Vector3 baseDirection)
    {
        Vector3 direction = Vector3.zero;

        switch (rotationType)
        {
            // �ɼ� 1) �ǰ��� -> ������ ������ ����Ʈ ����
            case VFXRotationType.targetToSource:

                direction = (attackSource.Position - targetPosition).normalized;
                break;

            // �ɼ� 2) ������ -> �ǰ��� ������ ����Ʈ ����
            case VFXRotationType.sourceToTarget:
                direction = (targetPosition - attackSource.Position).normalized;
                break;

            // �ɼ� 3) ���� ���� �ʿ� ����
            case VFXRotationType.None:
                return;
        }

        if (direction != Vector3.zero)
        {
            // ��ƼŬ �ý��� ������Ʈ�� ȸ��
            // Quaternion objectRotation = Quaternion.FromToRotation(baseDirection, direction);
            Quaternion objectRotation = Quaternion.LookRotation(direction); // �׽�Ʈ
            transform.rotation = objectRotation;

            // ������ ��ƼŬ�� ȸ��
            // ������Ʈ�� Y�� ȸ������ �������� ��ȯ, startRotationZ���� ������Ʈ�Ѵ�. 
            float billboardRotationInRadians = objectRotation.eulerAngles.y * Mathf.Deg2Rad;

            // ĳ�̵� ��� ����� startRotation�� �ݿ�
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
        
        // else���� �ʿ� ���� - null�̸� Destroy ȣ�� �Ұ���
    }
}


// ����Ʈ�� ���� ����
// �Ϲ������� �θ� ������Ʈ�� ������ ������ ������ �ʿ� ���� ��찡 ��κ��� ����! 
// �ϴ� �ǰ� ����Ʈ������ ��ƼŬ ������ �����ϱ� ���� ��������
// +Z �������� ����Ʈ�� ����ȴٰ� �������� ��
// targetToSource : �ǰ��� -> ������ �������� ����Ʈ�� �����
// sourceToTarget : ������ -> �ǰ��� �������� ����Ʈ�� �����
public enum VFXRotationType
{
    None,
    targetToSource,
    sourceToTarget,
}