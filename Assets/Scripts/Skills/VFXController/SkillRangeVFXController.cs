using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


// 스킬 범위의 시각적 효과를 관리
public class SkillRangeVFXController : MonoBehaviour, IPooledObject
{
    [Header("Effect Reference")]
    [SerializeField] protected ParticleSystem topEffect;
    [SerializeField] protected ParticleSystem bottomEffect;
    [SerializeField] protected ParticleSystem leftEffect;
    [SerializeField] protected ParticleSystem rightEffect;
    [SerializeField] protected Image topBoundary;
    [SerializeField] protected Image bottomBoundary;
    [SerializeField] protected Image leftBoundary;
    [SerializeField] protected Image rightBoundary;
    [SerializeField] protected Image floorImage;

    protected bool isInitialized = false;
    protected float fieldDuration;
    protected Dictionary<Vector2Int, (ParticleSystem effect, Image boundary)> directionEffects;
    protected readonly Vector2Int[] directions = new[] 
    {
        // 그리드 좌표는 좌측 상단이 (0, 0)이므로 Y좌표는 특히 이렇게 정의함
        Vector2Int.down,
        Vector2Int.up,
        Vector2Int.left,
        Vector2Int.right
    };

    private string poolTag;

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

    public void OnObjectSpawn()
    {
        // 초기에는 모든 이펙트 중지
        foreach (var pair in directionEffects.Values)
        {
            pair.effect.Stop();
            pair.boundary.gameObject.SetActive(false);
        }

        floorImage.gameObject.SetActive(false);
    }
    private void Update()
    {
        if (isInitialized)
        {
            if (fieldDuration < 0f)
            {
                StopAllVFXs();
                return;
            }

            fieldDuration -= Time.deltaTime;
        }
    }

    public void Initialize(Vector2Int position, HashSet<Vector2Int> effectRange, float duration, string tag)
    {
        poolTag = tag;
        isInitialized = true;

        // 즉발 스킬의 경우 duration = 0일 수 있음 -> 디폴트 필드 값을 1초로 지정
        fieldDuration = duration != 0f ? duration : 1f;

        // 이 위치에 타일이 없으면 실행 X
        if (!MapManager.Instance.CurrentMap.IsTileAt(position.x, position.y)) return;

        floorImage.gameObject.SetActive(true);

        foreach (var direction in directions)
        {
            // 이펙트 표시 여부 결정
            Vector2Int neighborPos = position + direction; 

            // 방향에 대한 이펙트 표시 여부
            bool showEffect = !effectRange.Contains(neighborPos) || // 스킬 범위 내에 있음
                !MapManager.Instance.CurrentMap.IsTileAt(neighborPos.x, neighborPos.y); // 실제로 타일이 있음

            var (effect, boundary) = directionEffects[direction];

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

        // 언덕에 이펙트 배치하는 상황
        Tile currentTile = MapManager.Instance.GetTile(position.x, position.y);
        if (currentTile != null && currentTile.data.terrain == TileData.TerrainType.Hill)
        {
            transform.position += Vector3.up * 0.2f;
        }

        // 스테이지 종료 시 파괴를 위한 이벤트 구독
        
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

    public void StopAllVFXs()
    {
        foreach (var pair in directionEffects.Values)
        {
            pair.effect.Stop();
            pair.boundary.gameObject.SetActive(false);
        }

        floorImage.gameObject.SetActive(false);
        ObjectPoolManager.Instance.ReturnToPool(poolTag, gameObject);
        isInitialized = false;
    }

    public void ForceRemove()
    {
        StopAllVFXs();
    }
}
