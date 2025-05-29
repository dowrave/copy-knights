using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

// 풀에서 꺼낸 전투 VFX의 실행과 오브젝트 풀링 관리
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

    // 그래프마다 활성화된 프로퍼티가 다름 / 이펙트 사용처가 더 많아지면 더 확장해야 함
    private void PlayVFXGraph()
    {
        if (vfx != null)
        {
            // GetHit에서는 AttackDirection, LifeTime을 사용
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

            // Attack에선 BaseDirection을 이용
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
