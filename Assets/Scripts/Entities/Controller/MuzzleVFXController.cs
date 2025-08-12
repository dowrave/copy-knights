using UnityEngine;
using System.Collections;
using UnityEngine.VFX;

// 오브젝트 풀로 돌아가는 기능만 수행함
public class MuzzleVFXController : MonoBehaviour
{
    private string poolTag;

    [Header("Settings")]
    [SerializeField] private ParticleSystem ps;
    [SerializeField] private VisualEffect vfxGraph;
    [SerializeField] private float vfxLifetime = 1f;

    public void Initialize(string poolTag)
    {
        this.poolTag = poolTag;

        if (ps != null)
        {
            ps.Play(true);
        }
        else if (vfxGraph != null)
        {
            vfxGraph.Play();
        }

        StartCoroutine(WaitAndReturnToPool(vfxLifetime));
    }

    private IEnumerator WaitAndReturnToPool(float duration)
    {
        yield return new WaitForSeconds(duration);

        ObjectPoolManager.Instance.ReturnToPool(poolTag, gameObject);
    }
}