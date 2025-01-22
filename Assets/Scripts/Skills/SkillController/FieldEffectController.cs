using System.Collections.Generic;
using UnityEngine;

// 범위 효과 스킬 내용 구현
public abstract class FieldEffectController : MonoBehaviour
{
    protected Operator caster; // 시전자
    protected Vector2Int centerPosition; // 중심 위치
    protected HashSet<Vector2Int> affectedTiles; // 실제 영향을 받는 타일들
    protected float fieldDuration; // 지속 시간
    protected float amountPerTick; // 필드 위의 타겟들에 적용되는 수치 (공격, 힐 등)
    protected float interval; // 틱이 적용되는 간격(초)

    protected float elapsedTime = 0f; // 경과 시간

    public virtual void Initialize(
        Operator caster, 
        Vector2Int centerPosition, 
        HashSet<Vector2Int> affectedTiles, 
        float fieldDuration,
        float amountPerTick,
        float interval
        )
    {
        this.caster = caster;
        this.centerPosition = centerPosition;
        this.affectedTiles = affectedTiles;
        this.fieldDuration = fieldDuration;
        this.amountPerTick = amountPerTick;
        this.interval = interval;
    }

    protected virtual void Update()
    {
        if (elapsedTime >= fieldDuration)
        {
            StopAndDestroyEffects();
        }

        elapsedTime += Time.deltaTime;
    }

    //protected abstract void UpdateTargets();

    //protected abstract void ApplyEffectToTarget();

    protected virtual void StopAndDestroyEffects()
    {
        Destroy(gameObject);
    }

    public virtual void ForceRemove()
    {
        StopAndDestroyEffects();
    }
}
