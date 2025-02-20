using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


// ��ų ������ �ð��� ȿ���� ����
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
        // �׸��� ��ǥ�� ���� ����� (0, 0)�̹Ƿ� Y��ǥ�� Ư�� �̷��� ������
        Vector2Int.down,
        Vector2Int.up,
        Vector2Int.left,
        Vector2Int.right
    };

    private string poolTag;

    private void Awake()
    {
        // ���⺰ ��ƼŬ �ý��� ����
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
        // �ʱ⿡�� ��� ����Ʈ ����
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

        // ��� ��ų�� ��� duration = 0�� �� ���� -> ����Ʈ �ʵ� ���� 1�ʷ� ����
        fieldDuration = duration != 0f ? duration : 1f;

        // �� ��ġ�� Ÿ���� ������ ���� X
        if (!MapManager.Instance.CurrentMap.IsTileAt(position.x, position.y)) return;

        floorImage.gameObject.SetActive(true);

        foreach (var direction in directions)
        {
            // ����Ʈ ǥ�� ���� ����
            Vector2Int neighborPos = position + direction; 

            // ���⿡ ���� ����Ʈ ǥ�� ����
            bool showEffect = !effectRange.Contains(neighborPos) || // ��ų ���� ���� ����
                !MapManager.Instance.CurrentMap.IsTileAt(neighborPos.x, neighborPos.y); // ������ Ÿ���� ����

            var (effect, boundary) = directionEffects[direction];

            if (showEffect) 
            {
                PrewarmTrailAndPlayVFX(effect); // effect.Play() ����
                boundary.gameObject.SetActive(true);
            }
            else
            {
                effect.Stop();
                boundary.gameObject.SetActive(false);
            }
        }

        // ����� ����Ʈ ��ġ�ϴ� ��Ȳ
        Tile currentTile = MapManager.Instance.GetTile(position.x, position.y);
        if (currentTile != null && currentTile.data.terrain == TileData.TerrainType.Hill)
        {
            transform.position += Vector3.up * 0.2f;
        }

        // �������� ���� �� �ı��� ���� �̺�Ʈ ����
        
    }

    // ��ų ���� ��� ǥ���� ���� ��ũ��Ʈ
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
