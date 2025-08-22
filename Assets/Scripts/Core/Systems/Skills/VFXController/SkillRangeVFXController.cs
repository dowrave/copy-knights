using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


// ��ų ������ �ð��� ȿ���� ����
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
    private Coroutine _lifeCycleCoroutine; // �����ֱ� �ڷ�ƾ ���� ����

    private void Awake()
    {
        // ���⺰ ��ƼŬ �ý��� ����
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

        // �ʱ� : ��� ����Ʈ ��Ȱ��ȭ
        foreach (var pair in directionEffects.Values)
        {
            pair.effect.gameObject.SetActive(false);
            pair.boundary.gameObject.SetActive(false);

            // ��ƼŬ �ý��� �ʱ�ȭ
            ParticleSystem ps = pair.effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); 
            }
        }
        
        floorImage.gameObject.SetActive(false);
    }

    // �ð� ȿ�� ���� �� ����
    public void Initialize(Vector2Int position, HashSet<Vector2Int> effectRange, float duration)
    {
        // ������ �������� �ڷ�ƾ ����
        if (_lifeCycleCoroutine != null) StopCoroutine(_lifeCycleCoroutine);

        // �ð� ȿ�� ����
        SetUpVisuals(position, effectRange);

        // �����ֱ� �ڷ�ƾ ����
        // ��� ��ų�̶�� ª�� �ð��� �����ְ� �����
        float lifeTime = (duration > 0f) ? duration : 1.0f;
        _lifeCycleCoroutine = StartCoroutine(LifeCycle(lifeTime));
    }

    private void SetUpVisuals(Vector2Int position, HashSet<Vector2Int> effectRange)
    {
        // ��ȿ���� ���� ��ġ�� �ƹ��͵� ǥ������ ����
        if (MapManager.Instance.CurrentMap == null || !MapManager.Instance.CurrentMap.IsTileAt(position.x, position.y))
        {
            floorImage.gameObject.SetActive(false);
            return;
        }

        floorImage.gameObject.SetActive(true);

        // ���⿡ ���� Ÿ�� �˻�� ����Ʈ ���� ���� ����
        foreach (var direction in directions)
        {
            Vector2Int neighborPos = position + direction;
            bool showEffect = !effectRange.Contains(neighborPos) || !MapManager.Instance.CurrentMap.IsTileAt(neighborPos.x, neighborPos.y);

            var (effectObject, boundary) = directionEffects[direction];

            effectObject.SetActive(showEffect);
            boundary.gameObject.SetActive(showEffect);

            // ��ƼŬ �ý������� ������ ��� ��ƼŬ �ý����� �����Ŵ
            ParticleSystem directionParticleSystem = effectObject.GetComponent<ParticleSystem>();
            if (directionParticleSystem != null)
            {
                directionParticleSystem.Play();
            }
        }

        // ��� Ÿ�� ��ġ ����
        Tile? currentTile = MapManager.Instance.GetTile(position.x, position.y);
        if (currentTile != null && currentTile.data.terrain == TileData.TerrainType.Hill)
        {
            transform.position += Vector3.up * 0.2f;
        }
    }

    // ���� �ֱ⸦ �����ϴ� ���� �ڷ�ƾ
    private IEnumerator LifeCycle(float lifeTime)
    {
        yield return new WaitForSeconds(lifeTime);
        ReturnToPool();
    }


    private void ReturnToPool()
    {
        _lifeCycleCoroutine = null;
        // ������Ʈ Ǯ �Ŵ������� ���������޶�� ��û
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
