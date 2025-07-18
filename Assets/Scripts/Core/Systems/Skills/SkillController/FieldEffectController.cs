using System.Collections.Generic;
using System.Collections;
using UnityEngine;

// ���� ȿ�� ��ų ���� ����
public abstract class FieldEffectController : MonoBehaviour, IPooledObject
{
    protected Operator? caster; // ������
    protected HashSet<Vector2Int> affectedTiles = new HashSet<Vector2Int>(); // ���� ������ �޴� Ÿ�ϵ�
    protected float amountPerTick; // �ʵ� ���� Ÿ�ٵ鿡 ����Ǵ� ��ġ (����, �� ��)
    protected GameObject hitEffectPrefab = default!;
    protected string hitEffectTag = string.Empty;
    protected string poolTag = string.Empty;

    // ������� ��� ��ųʸ�
    protected Dictionary<UnitEntity, List<Buff>> affectedTargets = new Dictionary<UnitEntity, List<Buff>>();

    public void OnObjectSpawn(string tag)
    {
        this.poolTag = tag;
        affectedTargets.Clear();
    }


    public virtual void Initialize(
        Operator caster,
        HashSet<Vector2Int> affectedTiles,
        float fieldDuration,
        float amountPerTick,
        float interval,
        GameObject hitEffectPrefab,
        string hitEffectTag
        )
    {
        this.caster = caster;
        this.affectedTiles = affectedTiles;
        this.amountPerTick = amountPerTick;
        this.hitEffectPrefab = hitEffectPrefab;
        this.hitEffectTag = hitEffectTag;

        StartCoroutine(FieldRoutine(fieldDuration, interval));
    }

    // Update ��� ��� ������ ó���ϴ� ���� �ڷ�ƾ
    private IEnumerator FieldRoutine(float duration, float interval)
    {
        float elapsedTime = 0f;
        float lastTickTime = -interval; // �������ڸ��� ù ƽ�� �ߵ�

        while (elapsedTime < duration)
        {
            // �����ڰ� ������� ����
            if (caster == null) break;

            // ���� �� ��� üũ
            CheckTargetsInField();

            if (Time.time >= lastTickTime + interval)
            {
                ApplyPeriodicEffect();
                lastTickTime = Time.time;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        CleanUpAndReturnToPool();
    }

    protected abstract void CheckTargetsInField();

    // �ʵ忡 ���� ��󿡰� ��� �����ϴ� ȿ��
    protected abstract void ApplyInitialEffect(UnitEntity target);
    // ���� �������� ����Ǵ� ȿ��
    protected abstract void ApplyPeriodicEffect();

    protected virtual bool IsTargetInField(UnitEntity target)
    {
        if (target == null) return false;

        Vector2Int targetPos = MapManager.Instance!.ConvertToGridPosition(target.transform.position);
        return affectedTiles.Contains(targetPos);
    }

    protected virtual void CleanUpAndReturnToPool()
    {
        foreach (var pair in affectedTargets)
        {
            if (pair.Key != null)
            {
                foreach (var effect in pair.Value)
                {
                    pair.Key.RemoveBuff(effect);
                }
            }
        }

        affectedTargets.Clear();

        Destroy(gameObject);
    }
}
