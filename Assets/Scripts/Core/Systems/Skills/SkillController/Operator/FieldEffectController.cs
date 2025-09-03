using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;

// ���� ȿ�� ��ų ���� ����
public abstract class FieldEffectController : MonoBehaviour, IPooledObject
{
    protected UnitEntity? caster; // ������
    protected IReadOnlyCollection<Vector2Int> skillRangeGridPositions = new HashSet<Vector2Int>(); // ���� ������ �޴� Ÿ�ϵ�
    protected float fieldDuration; // �ʵ� ���� �ð�
    protected float tickDamageRatio; // ��Ʈ ����� ����
    protected float interval; // ��Ʈ ����� ����
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
        UnitEntity caster,
        IReadOnlyCollection<Vector2Int> skillRangeGridPositions,
        float fieldDuration,
        float tickDamageRatio,
        float interval,
        GameObject hitEffectPrefab,
        string hitEffectTag
        )
    {
        this.caster = caster;
        this.skillRangeGridPositions = skillRangeGridPositions;
        this.fieldDuration = fieldDuration;
        this.interval = interval;
        this.tickDamageRatio = tickDamageRatio;
        this.hitEffectPrefab = hitEffectPrefab;
        this.hitEffectTag = hitEffectTag;
    }

    // Update ��� ��� ������ ó���ϴ� ���� �ڷ�ƾ
    protected virtual IEnumerator FieldRoutine(float duration, float interval)
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
        return skillRangeGridPositions.Contains(targetPos);
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

        // Destroy(gameObject);
        gameObject.SetActive(false);
    }
}
