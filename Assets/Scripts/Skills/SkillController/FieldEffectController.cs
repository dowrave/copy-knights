using System.Collections.Generic;
using UnityEngine;

// ���� ȿ�� ��ų ���� ����
public abstract class FieldEffectController : MonoBehaviour
{
    protected Operator? caster; // ������
    protected Vector2Int centerPosition; // �߽� ��ġ
    protected HashSet<Vector2Int> affectedTiles = new HashSet<Vector2Int>(); // ���� ������ �޴� Ÿ�ϵ�
    protected float fieldDuration; // ���� �ð�
    protected float amountPerTick; // �ʵ� ���� Ÿ�ٵ鿡 ����Ǵ� ��ġ (����, �� ��)
    protected float interval; // ƽ�� ����Ǵ� ����(��)

    protected float elapsedTime; // ��� �ð�
    protected float lastTickTime; // ������ ȿ�� ���� �ð�

    protected GameObject hitEffectPrefab = default!;
    protected SkillRangeVFXController rangeVFXController = default!;

    // ������� ��� ��ųʸ�
    protected Dictionary<UnitEntity, List<CrowdControl>> affectedTargets = new Dictionary<UnitEntity, List<CrowdControl>>();

    public virtual void Initialize(
        Operator caster,
        Vector2Int centerPosition,
        HashSet<Vector2Int> affectedTiles,
        float fieldDuration,
        float amountPerTick,
        float interval,
        GameObject hitEffectPrefab
        )
    {
        this.caster = caster;
        this.centerPosition = centerPosition;
        this.affectedTiles = affectedTiles;
        this.fieldDuration = fieldDuration;
        this.amountPerTick = amountPerTick;
        this.interval = interval;
        this.hitEffectPrefab = hitEffectPrefab;

        caster.OnOperatorDied += HandleOperatorDied; 
    }

    protected virtual void Update()
    {
        if (elapsedTime >= fieldDuration)
        {
            StopAndDestroyEffects();
        }

        // ���� �� ��� üũ
        CheckTargetsInField();

        // �ֱ��� ȿ�� ����
        if (Time.time >= lastTickTime + interval)
        {
            ApplyPeriodicEffect();
            lastTickTime = Time.time;
        }

        elapsedTime += Time.deltaTime;
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

    protected virtual void StopAndDestroyEffects()
    {
        foreach (var pair in affectedTargets)
        {
            if (pair.Key != null)
            {
                foreach (var effect in pair.Value)
                {
                    pair.Key.RemoveCrowdControl(effect);
                }
            }
        }

        affectedTargets.Clear();

        if (caster != null) caster.OnOperatorDied -= HandleOperatorDied; 

        Destroy(gameObject);
    }

    public virtual void ForceRemove()
    {
        StopAndDestroyEffects();
    }

    protected virtual void HandleOperatorDied(Operator op)
    {
        StopAndDestroyEffects();
    }

    private void OnDisable()
    {
        if (caster != null)
        {
            caster.OnOperatorDied -= HandleOperatorDied;
        }
    }
}
