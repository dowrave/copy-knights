using System.Collections.Generic;
using UnityEngine;

// 스킬 범위의 시각적 효과를 관리
public class SkillRangeEffect : MonoBehaviour, IPooledObject
{
    [Header("Effect Reference")]
    [SerializeField] private ParticleSystem topEffect;
    [SerializeField] private ParticleSystem bottomEffect;
    [SerializeField] private ParticleSystem leftEffect;
    [SerializeField] private ParticleSystem rightEffect;

    [Header("Floor")]
    [SerializeField] private MeshRenderer floorRenderer;
    [SerializeField] private float baseAlpha;

    [Header("Effect Color")]
    [SerializeField] private Color effectColor;

    private float fieldDuration;
    private Dictionary<Vector2Int, ParticleSystem> directionEffects;
    private readonly Vector2Int[] directions = new[]
    {
        // 그리드 좌표는 좌측 상단이 (0, 0)이므로 Y좌표는 특히 이렇게 정의함
        Vector2Int.down,
        Vector2Int.up,
        Vector2Int.left,
        Vector2Int.right
    };

    private string poolTag;
    private static MaterialPropertyBlock propertyBlock;
    private static readonly int colorID = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        // 방향별 파티클 시스템 매핑
        directionEffects = new Dictionary<Vector2Int, ParticleSystem>
        {
            { Vector2Int.down, topEffect },
            { Vector2Int.up, bottomEffect },
            { Vector2Int.left, leftEffect },
            { Vector2Int.right, rightEffect }
        };
    }

    public void OnObjectSpawn()
    {
        // 초기에는 모든 이펙트 중지
        foreach (var effect in directionEffects.Values)
        {
            effect.Stop();
        }

        // 바닥도 초기엔 비활성화
        floorRenderer.gameObject.SetActive(false);
    }
    private void Update()
    {
        if (fieldDuration < 0f)
        {
            StopAllEffects();
        }

        fieldDuration -= Time.deltaTime;
    }

    public void Initialize(Vector2Int position, HashSet<Vector2Int> effectRange, Color effectColor, float duration, string tag)
    {
        poolTag = tag;

        // 이 위치에 타일이 없으면 실행 X
        if (!MapManager.Instance.CurrentMap.IsTileAt(position.x, position.y)) return;

        foreach (var direction in directions)
        {
            // 이펙트 표시 여부 결정
            Vector2Int neighborPos = position + direction; 

            // 방향에 대한 이펙트 표시 여부
            bool showEffect = !effectRange.Contains(neighborPos) || // 스킬 범위 내에 있음
                !MapManager.Instance.CurrentMap.IsTileAt(neighborPos.x, neighborPos.y); // 실제로 타일이 있음

            var effect = directionEffects[direction];

            if (showEffect) 
            {
                var main = effect.main;
                main.startColor = effectColor;
                effect.Play();
            }
            else
            {
                effect.Stop();
            }
        }

        // 바닥 이펙트 조정
        floorRenderer.gameObject.SetActive(true);
        if (floorRenderer != null)
        {
            propertyBlock.SetColor(colorID, new Color(effectColor.r, effectColor.g, effectColor.b, baseAlpha));
            floorRenderer.SetPropertyBlock(propertyBlock);
        }

        // 언덕에 이펙트 배치하는 상황
        Tile currentTile = MapManager.Instance.GetTile(position.x, position.y);
        if (currentTile != null && currentTile.data.terrain == TileData.TerrainType.Hill)
        {
            transform.position += Vector3.up * 0.2f;
        }

        this.fieldDuration = duration;
    }

    public void StopAllEffects()
    {
        foreach (var effect in directionEffects.Values)
        {
            effect.Stop();
        }
        floorRenderer.gameObject.SetActive(false);
        Debug.Log($"SkillRangeEffect.StopAllEfffects - PoolTag : {poolTag}");
        ObjectPoolManager.Instance.ReturnToPool(poolTag, gameObject);
    }


    private void OnDisable()
    {
        StopAllEffects();
    }
}
