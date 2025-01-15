using System.Collections.Generic;
using UnityEngine;

// ��ų ������ �ð��� ȿ���� ����
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
        // �׸��� ��ǥ�� ���� ����� (0, 0)�̹Ƿ� Y��ǥ�� Ư�� �̷��� ������
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

        // ���⺰ ��ƼŬ �ý��� ����
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
        // �ʱ⿡�� ��� ����Ʈ ����
        foreach (var effect in directionEffects.Values)
        {
            effect.Stop();
        }

        // �ٴڵ� �ʱ⿣ ��Ȱ��ȭ
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

        // �� ��ġ�� Ÿ���� ������ ���� X
        if (!MapManager.Instance.CurrentMap.IsTileAt(position.x, position.y)) return;

        foreach (var direction in directions)
        {
            // ����Ʈ ǥ�� ���� ����
            Vector2Int neighborPos = position + direction; 

            // ���⿡ ���� ����Ʈ ǥ�� ����
            bool showEffect = !effectRange.Contains(neighborPos) || // ��ų ���� ���� ����
                !MapManager.Instance.CurrentMap.IsTileAt(neighborPos.x, neighborPos.y); // ������ Ÿ���� ����

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

        // �ٴ� ����Ʈ ����
        floorRenderer.gameObject.SetActive(true);
        if (floorRenderer != null)
        {
            propertyBlock.SetColor(colorID, new Color(effectColor.r, effectColor.g, effectColor.b, baseAlpha));
            floorRenderer.SetPropertyBlock(propertyBlock);
        }

        // ����� ����Ʈ ��ġ�ϴ� ��Ȳ
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
