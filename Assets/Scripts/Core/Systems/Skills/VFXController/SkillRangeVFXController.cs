using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


// 스킬 범위의 시각적 효과를 관리
public class SkillRangeVFXController : MonoBehaviour, IPooledObject
{
    [Header("Effect Reference")]
    [SerializeField] protected GameObject topEffectObject = default!;
    [SerializeField] protected GameObject bottomEffectObject = default!;
    [SerializeField] protected GameObject leftEffectObject = default!;
    [SerializeField] protected GameObject rightEffectObject = default!;
    [SerializeField] protected Image topBoundary = default!;
    [SerializeField] protected Image bottomBoundary = default!;
    [SerializeField] protected Image leftBoundary = default!;
    [SerializeField] protected Image rightBoundary = default!;
    [SerializeField] protected Image floorImage = default!;

    protected Dictionary<Vector2Int, (GameObject effect, Image boundary)> directionEffects = new Dictionary<Vector2Int, (GameObject effect, Image boundary)>();
    protected readonly Vector2Int[] directions = new[] { Vector2Int.down, Vector2Int.up, Vector2Int.left, Vector2Int.right };

    private string poolTag = string.Empty;
    private Coroutine _lifeCycleCoroutine; // 생명주기 코루틴 추적 변수

    private void Awake()
    {
        // 방향별 파티클 시스템 매핑
        directionEffects = new Dictionary<Vector2Int, (GameObject, Image)>
        {
            { Vector2Int.down, (topEffectObject, topBoundary) },
            { Vector2Int.up, (bottomEffectObject, bottomBoundary) },
            { Vector2Int.left, (leftEffectObject, leftBoundary) },
            { Vector2Int.right, (rightEffectObject, rightBoundary) }
        };
    }

    public void OnObjectSpawn(string tag)
    {
        this.poolTag = tag;

        // 초기 : 모든 이펙트 비활성화
        foreach (var pair in directionEffects.Values)
        {
            pair.effect.gameObject.SetActive(false);
            pair.boundary.gameObject.SetActive(false);

            // 파티클 시스템 초기화
            ParticleSystem ps = pair.effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); 
            }
        }
        
        floorImage.gameObject.SetActive(false);
    }

    // 시각 효과 설정 및 실행
    public void Initialize(Vector2Int position, HashSet<Vector2Int> effectRange, float duration)
    {
        // 이전에 실행중인 코루틴 중지
        if (_lifeCycleCoroutine != null) StopCoroutine(_lifeCycleCoroutine);

        // 시각 효과 설정
        SetUpVisuals(position, effectRange);

        // 생명주기 코루틴 시작
        // 즉발 스킬이라면 짧은 시간만 보여주고 사라짐
        float lifeTime = (duration > 0f) ? duration : 1.0f;
        _lifeCycleCoroutine = StartCoroutine(LifeCycle(lifeTime));
    }

    private void SetUpVisuals(Vector2Int position, HashSet<Vector2Int> effectRange)
    {
        // 유효하지 않은 위치는 아무것도 표시하지 않음
        if (MapManager.Instance.CurrentMap == null || !MapManager.Instance.CurrentMap.IsTileAt(position.x, position.y))
        {
            floorImage.gameObject.SetActive(false);
            return;
        }

        floorImage.gameObject.SetActive(true);

        // 방향에 따른 타일 검사로 이펙트 실행 여부 결정
        foreach (var direction in directions)
        {
            Vector2Int neighborPos = position + direction;
            bool showEffect = !effectRange.Contains(neighborPos) || !MapManager.Instance.CurrentMap.IsTileAt(neighborPos.x, neighborPos.y);

            var (effectObject, boundary) = directionEffects[direction];

            effectObject.SetActive(showEffect);
            boundary.gameObject.SetActive(showEffect);

            // 파티클 시스템으로 구현된 경우 파티클 시스템을 실행시킴
            ParticleSystem directionParticleSystem = effectObject.GetComponent<ParticleSystem>();
            if (directionParticleSystem != null)
            {
                directionParticleSystem.Play();
            }
        }

        // 언덕 타일 위치 보정
        Tile? currentTile = MapManager.Instance.GetTile(position.x, position.y);
        if (currentTile != null && currentTile.data.terrain == TileData.TerrainType.Hill)
        {
            transform.position += Vector3.up * 0.2f;
        }
    }

    // 생명 주기를 관리하는 단일 코루틴
    private IEnumerator LifeCycle(float lifeTime)
    {
        yield return new WaitForSeconds(lifeTime);
        ReturnToPool();
    }


    private void ReturnToPool()
    {
        _lifeCycleCoroutine = null;
        // 오브젝트 풀 매니저에게 돌려보내달라고 요청
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
