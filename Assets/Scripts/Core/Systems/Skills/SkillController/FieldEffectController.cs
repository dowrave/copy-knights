using System.Collections.Generic;
using System.Collections;
using UnityEngine;

// 범위 효과 스킬 내용 구현
public abstract class FieldEffectController : MonoBehaviour, IPooledObject
{
    protected Operator? caster; // 시전자
    protected HashSet<Vector2Int> affectedTiles = new HashSet<Vector2Int>(); // 실제 영향을 받는 타일들
    protected float amountPerTick; // 필드 위의 타겟들에 적용되는 수치 (공격, 힐 등)
    protected GameObject hitEffectPrefab = default!;
    protected string hitEffectTag = string.Empty;
    protected string poolTag = string.Empty;

    // 영향받은 대상 딕셔너리
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

    // Update 대신 모든 로직을 처리하는 메인 코루틴
    private IEnumerator FieldRoutine(float duration, float interval)
    {
        float elapsedTime = 0f;
        float lastTickTime = -interval; // 시작하자마자 첫 틱이 발동

        while (elapsedTime < duration)
        {
            // 시전자가 사라지면 종료
            if (caster == null) break;

            // 범위 내 대상 체크
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
