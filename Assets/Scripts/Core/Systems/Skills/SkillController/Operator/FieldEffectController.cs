using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;

// 범위 효과 스킬 내용 구현
public abstract class FieldEffectController : MonoBehaviour, IPooledObject
{
    protected UnitEntity? caster; // 시전자
    protected IReadOnlyCollection<Vector2Int> skillRangeGridPositions = new HashSet<Vector2Int>(); // 실제 영향을 받는 타일들
    protected float fieldDuration; // 필드 지속 시간
    protected float tickDamageRatio; // 도트 대미지 배율
    protected float interval; // 도트 대미지 간격
    protected GameObject hitEffectPrefab = default!;
    protected string hitEffectTag = string.Empty;
    protected string poolTag = string.Empty;

    protected Coroutine _currentCoroutine; 

    // 영향받은 대상 딕셔너리
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
        ) {}

    protected virtual void InitializeFields(        
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

    protected virtual void InitializeCoroutine()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }

        _currentCoroutine = StartCoroutine(FieldRoutine(fieldDuration, interval));
    }

    // Update 대신 모든 로직을 처리하는 메인 코루틴
    // 말단 클래스들에서 각각 실행시키도록 합시다 
    protected virtual IEnumerator FieldRoutine(float duration, float interval)
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
        // gameObject.SetActive(false);
        ObjectPoolManager.Instance!.ReturnToPool(poolTag, gameObject);
    }
}
