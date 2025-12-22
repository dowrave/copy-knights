using System.Collections;
using UnityEngine;

// 코루틴에 의한 동작에서, 부모 오브젝트가 파괴되거나 비활성화됐을 때 파티클 시스템이 실행 중이었다면 풀 반환이 되지 않는 상황이 발생함
// 이 스크립트는 생성된 파티클 시스템이 시간이 지나면 스스로 풀로 돌아가도록 구현됨
// 비슷한 게 SkillRangeVFXController나 CombatVFXController에도 있는데 얘가 가장 간소화된 버전
public class SelfReturnVFXController : MonoBehaviour, IPooledObject
{
    [Header("Effect(Image) Reference")]
    [SerializeField] protected ParticleSystem ps = default!;
    [SerializeField] protected bool isGroundVFX = false;

    protected UnitEntity? caster;
    protected string poolTag = string.Empty;
    protected Coroutine _lifeCycleCoroutine; // 생명주기 코루틴 추적 변수

    public void OnObjectSpawn(string tag)
    {
        this.poolTag = tag;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // 시각 효과 설정 및 실행
    public virtual void Initialize(float duration, UnitEntity? caster = null)
    {
        Vector2Int nowGridPosition = MapManager.Instance.ConvertToGridPosition(transform.position);

        // 유효하지 않은 위치는 아무것도 표시하지 않음
        if (MapManager.Instance.CurrentMap == null || !MapManager.Instance.CurrentMap.IsTileAt(nowGridPosition.x, nowGridPosition.y))
        {
            Logger.LogWarning("파티클 시스템 플레이되지 않음");
            ps.gameObject.SetActive(false);
            return;
        }

        FixVisuals(nowGridPosition);

        // 생명주기 코루틴 시작
        // 즉발 스킬이라면 짧은 시간만 보여주고 사라짐
        ps.Play(true);
        float lifeTime = (duration > 0f) ? duration : 1.0f;
        _lifeCycleCoroutine = StartCoroutine(LifeCycle(lifeTime));

        if (caster != null)
        {
            this.caster = caster;
            caster.OnDeathStarted += ReturnToPool;
        }
    }

    protected void FixVisuals(Vector2Int position)
    {
        // 언덕 타일 위치 보정
        Tile? currentTile = MapManager.Instance.GetTile(position.x, position.y);
        if (currentTile != null && currentTile.TileData.Terrain == TileData.TerrainType.Hill)
        {
            transform.position += Vector3.up * 0.2f;
        }
    }

    // 생명 주기를 관리하는 단일 코루틴
    protected IEnumerator LifeCycle(float lifeTime)
    {
        yield return new WaitForSeconds(lifeTime);
        ReturnToPool();
    }

    // 이벤트 감지해서 풀로 반환
    // entity가 있는 경우에만 실행
    protected void ReturnToPool(UnitEntity entity)
    {
        ReturnToPool();
        caster.OnDeathStarted -= ReturnToPool;
    }

    protected virtual void ReturnToPool()
    {
        _lifeCycleCoroutine = null;
        ObjectPoolManager.Instance?.ReturnToPool(poolTag, gameObject);
    }

    public void ForceRemove()
    {
        if (_lifeCycleCoroutine != null)
        {
            StopCoroutine(_lifeCycleCoroutine);
        }
        ReturnToPool();
    }
}
