using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ��ų ������ �ð��� ȿ���� ����
public class SkillRangeVFXController : MonoBehaviour, IPooledObject, IEffectController
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

    private bool isInitialized = false;
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

    public void Initialize(Vector2Int position, HashSet<Vector2Int> effectRange, Color effectColor, float duration, string tag)
    {
        poolTag = tag;
        isInitialized = true;

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
                PrewarmTrailAndPlayVFX(effect); // effect.Play() ����
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

        // ��� ��ų�� ��� duration = 0�� �� ���� -> ����Ʈ �ʵ� ���� 1�ʷ� ����
        if (duration == 0f)
        {
            fieldDuration = 1f;
        }
    }

    // ��ų ���� ��� ǥ���� ���� ��ũ��Ʈ
    private void PrewarmTrailAndPlayVFX(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.startColor = effectColor;
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
        foreach (var effect in directionEffects.Values)
        {
            effect.Stop();
        }
        floorRenderer.gameObject.SetActive(false);
        ObjectPoolManager.Instance.ReturnToPool(poolTag, gameObject);
        isInitialized = false;
    }

    public void ForceRemove()
    {
        StopAllVFXs();
    }
}
