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

    ParticleSystem? ps; 
    VisualEffect? vfx;

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

        ps = GetComponent<ParticleSystem>();
        vfx = GetComponent<VisualEffect>();

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
            ps.Play();
        }
    }

    private IEnumerator WaitAndReturnToPool(float duration = 1f)
    {
        yield return new WaitForSeconds(duration);

        if (gameObject != null)
        {
            ObjectPoolManager.Instance!.ReturnToPool(effectTag, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
