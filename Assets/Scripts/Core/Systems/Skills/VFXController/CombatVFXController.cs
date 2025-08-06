using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

// Ǯ���� ���� ���� VFX�� ����� ������Ʈ Ǯ�� ����
public class CombatVFXController : MonoBehaviour
{
    private AttackSource attackSource;
    private UnitEntity? target; // null�� �� ����
    private Vector3 targetPosition;
    private string effectTag = string.Empty;
    private float effectDuration;

    [Header("Assign One")]
    [SerializeField] private ParticleSystem? ps; 
    [SerializeField] private VisualEffect? vfx;

    [Header("Particle System Options")]
    [SerializeField] private VFXRotationType rotationType = VFXRotationType.None;

    private void Awake()
    {
        // ps = GetComponent<ParticleSystem>();
        vfx = GetComponent<VisualEffect>(); // ps�� ���� �� ���� �Ҵ�
    }

    // Ÿ���� ���� �� - ��ġ ������ �̾Ƴ���.
    public void Initialize(AttackSource attackSource, UnitEntity target, string effectTag, float effectDuration = 1f)
    {
        Initialize(attackSource, target.transform.position, effectTag, effectDuration, target);

    }

    public void Initialize(AttackSource attackSource, Vector3 targetPosition, string effectTag, float effectDuration = 1f, UnitEntity? target = null)
    {
        this.attackSource = attackSource;
        this.targetPosition = targetPosition;
        this.target = target; 
        this.effectTag = effectTag;
        this.effectDuration = effectDuration;

        if (vfx != null)
        {
            PlayVFXGraph();
        }
        else if (ps != null)
        {
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
        if (ps != null)
        {
            Vector3 baseDirection = Vector3.forward; // ��� ����Ʈ�� +Z������ ����ȴٰ� ������
            switch (rotationType)
            {
                // �ɼ� 1) �ǰ��� -> ������ ������ ����Ʈ ����
                case VFXRotationType.targetToSource:

                    Vector3 directionToSource = (attackSource.Position - targetPosition).normalized;
                    if (directionToSource != Vector3.zero)
                    {
                        Quaternion rotation = Quaternion.FromToRotation(baseDirection, directionToSource);
                        transform.rotation = rotation;
                    }                
                    break;

                // �ɼ� 2) ������ -> �ǰ��� ������ ����Ʈ ����
                case VFXRotationType.sourceToTarget:
                    Vector3 directionToTarget = (targetPosition - attackSource.Position).normalized;
                    if (directionToTarget != Vector3.zero)
                    {
                        Quaternion rotation = Quaternion.FromToRotation(baseDirection, directionToTarget);
                        transform.rotation = rotation;
                    }
                    break;

                // �ɼ� 3) ���� ���� �ʿ� ����
                case VFXRotationType.None:
                    break;
            }

            ps.Play(true); // true �� ��� �ڽ� ����Ʈ���� �Ѳ����� �����
        }
    }

    private IEnumerator WaitAndReturnToPool(float duration = 1f)
    {
        yield return new WaitForSeconds(duration);

        if (gameObject != null)
        {
            ObjectPoolManager.Instance!.ReturnToPool(effectTag, gameObject);
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