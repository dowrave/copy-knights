using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

// Ǯ���� ���� ���� VFX�� ����� ������Ʈ Ǯ�� ����
public class CombatVFXController : MonoBehaviour
{
    private ICombatEntity.AttackSource attackSource;
    private UnitEntity target;
    private string effectTag;
    float effectDuration;

    ParticleSystem ps; 
    VisualEffect vfx;

    public void Initialize(ICombatEntity.AttackSource attackSource, UnitEntity target, string effectTag, float effectDuration = 1f)
    {
        this.attackSource = attackSource;
        this.target = target;
        this.effectTag = effectTag;
        this.effectDuration = effectDuration;

        vfx = GetComponent<VisualEffect>();
        ps = GetComponent<ParticleSystem>();

        if (vfx != null)
        {
            PlayVFXGraph();
        }
        else if (ps != null)
        {
            PlayPS();
        }

        StartCoroutine(WaitAndReturnToPool(effectDuration));
    }

    // �׷������� Ȱ��ȭ�� ������Ƽ�� �ٸ� / ����Ʈ ���ó�� �� �������� �� Ȯ���ؾ� ��
    private void PlayVFXGraph()
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
        if (vfx != null && vfx.HasVector3("BaseDirection"))
        {
            Vector3 baseDirection = vfx.GetVector3("BaseDirection");
            Vector3 attackDirection = (target.transform.position - transform.position).normalized;
            Quaternion rotation = Quaternion.FromToRotation(baseDirection, attackDirection);
            gameObject.transform.rotation = rotation;
        }

        vfx.Play();
    }

    private void PlayPS()
    {
        ps.Play();
    }

    private IEnumerator WaitAndReturnToPool(float effectDuration = 1f)
    {
        yield return new WaitForSeconds(effectDuration);

        if (gameObject != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(tag, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
