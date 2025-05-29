using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

// Ǯ���� ���� ���� VFX�� ����� ������Ʈ Ǯ�� ����
public class CombatVFXController : MonoBehaviour
{
    private ICombatEntity.AttackSource attackSource;
    private UnitEntity target = default!;
    private string effectTag = string.Empty;
    private float effectDuration;

    ParticleSystem? ps; 
    VisualEffect? vfx;

    public void Initialize(ICombatEntity.AttackSource attackSource, UnitEntity target, string effectTag, float effectDuration = 1f)
    {
        
        this.attackSource = attackSource;
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

        StartCoroutine(WaitAndReturnToPool(effectDuration));
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
                Vector3 attackDirection = (target.transform.position - transform.position).normalized;
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

    private IEnumerator WaitAndReturnToPool(float effectDuration = 1f)
    {
        yield return new WaitForSeconds(effectDuration);

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
