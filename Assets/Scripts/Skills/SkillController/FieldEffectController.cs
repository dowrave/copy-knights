using System.Collections.Generic;
using UnityEngine;

// 범위 효과 스킬 내용 구현
public abstract class FieldEffectController : MonoBehaviour
{
    protected Operator? caster; // 시전자
    protected Vector2Int centerPosition; // 중심 위치
    protected HashSet<Vector2Int> affectedTiles = new HashSet<Vector2Int>(); // 실제 영향을 받는 타일들
    protected float fieldDuration; // 지속 시간
    protected float amountPerTick; // 필드 위의 타겟들에 적용되는 수치 (공격, 힐 등)
    protected float interval; // 틱이 적용되는 간격(초)

    protected float elapsedTime; // 경과 시간
    protected float lastTickTime; // 마지막 효과 적용 시간

    protected GameObject hitEffectPrefab = default!;
    protected SkillRangeVFXController rangeVFXController = default!;

    // 영향받은 대상 딕셔너리
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

        // 범위 내 대상 체크
        CheckTargetsInField();

        // 주기적 효과 적용
        if (Time.time >= lastTickTime + interval)
        {
            ApplyPeriodicEffect();
            lastTickTime = Time.time;
        }

        elapsedTime += Time.deltaTime;
    }

    protected abstract void CheckTargetsInField();

    // 필드에 들어온 대상에게 즉시 적용하는 효과
    protected abstract void ApplyInitialEffect(UnitEntity target);
    // 일정 간격으로 적용되는 효과
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
