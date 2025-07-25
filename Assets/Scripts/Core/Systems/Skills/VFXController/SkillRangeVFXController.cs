using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


// 스킬 범위의 시각적 효과를 관리
public class SkillRangeVFXController : MonoBehaviour, IPooledObject
{
    [Header("Effect Reference")]
    [SerializeField] protected ParticleSystem topEffect = default!;
    [SerializeField] protected ParticleSystem bottomEffect = default!;
    [SerializeField] protected ParticleSystem leftEffect = default!;
    [SerializeField] protected ParticleSystem rightEffect = default!;
    [SerializeField] protected Image topBoundary = default!;
    [SerializeField] protected Image bottomBoundary = default!;
    [SerializeField] protected Image leftBoundary = default!;
    [SerializeField] protected Image rightBoundary = default!;
    [SerializeField] protected Image floorImage = default!;

    protected Dictionary<Vector2Int, (ParticleSystem effect, Image boundary)> directionEffects = new Dictionary<Vector2Int, (ParticleSystem effect, Image boundary)>();
    protected readonly Vector2Int[] directions = new[] { Vector2Int.down, Vector2Int.up, Vector2Int.left, Vector2Int.right };

    private string poolTag = string.Empty;
    private Coroutine _lifeCycleCoroutine; // 생명주기 코루틴 추적 변수

    private void Awake()
    {
        // 방향별 파티클 시스템 매핑
        directionEffects = new Dictionary<Vector2Int, (ParticleSystem, Image)>
        {
            { Vector2Int.down, (topEffect, topBoundary) },
            { Vector2Int.up, (bottomEffect, bottomBoundary) },
            { Vector2Int.left, (leftEffect, leftBoundary) },
            { Vector2Int.right, (rightEffect, rightBoundary) }
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
        }

        floorImage.gameObject.SetActive(false);
        
        // 초기에는 모든 이펙트 중지
        // foreach (var pair in directionEffects.Values)
        // {
        //     pair.effect.Stop();
        //     pair.boundary.gameObject.SetActive(false);
        // }

        // floorImage.gameObject.SetActive(false);
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
            return;
        }

        floorImage.gameObject.SetActive(false);

        // 방향에 따른 타일 검사로 이펙트 실행 여부 결정
        foreach (var direction in directions)
        {
            Vector2Int neighborPos = position + direction;
            bool showEffect = !effectRange.Contains(neighborPos) || !MapManager.Instance.CurrentMap.IsTileAt(neighborPos.x, neighborPos.y);

            var (effect, boundary) = directionEffects[direction];

            effect.gameObject.SetActive(showEffect);
            boundary.gameObject.SetActive(showEffect);

            if (showEffect)
            {
                PrewarmTrailAndPlayVFX(effect); // effect.Play() 포함
                boundary.gameObject.SetActive(true);
            }
            else
            {
                effect.Stop();
                boundary.gameObject.SetActive(false);
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

    // 스킬 범위 즉시 표현을 위한 스크립트
    private void PrewarmTrailAndPlayVFX(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        ps.Play();

        StartCoroutine(ShowEffectAfterPrewarm(main));
    }

    private IEnumerator ShowEffectAfterPrewarm(ParticleSystem.MainModule main)
    {
        main.simulationSpeed = 100f;
        yield return new WaitForSeconds(0.01f);
        main.simulationSpeed = 1f;
    }
}
