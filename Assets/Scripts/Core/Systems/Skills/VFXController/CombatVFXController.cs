using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

// 풀에서 꺼낸 전투 VFX의 실행과 오브젝트 풀링 관리
public class CombatVFXController : MonoBehaviour
{
    private AttackSource attackSource;
    private UnitEntity? target; // null일 수 있음
    private Vector3 targetPosition;
    private string effectTag = string.Empty;
    private float effectDuration;

    ParticleSystem? ps; 
    VisualEffect? vfx;

    // 타겟이 있을 때 - 위치 정보만 뽑아낸다.
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
