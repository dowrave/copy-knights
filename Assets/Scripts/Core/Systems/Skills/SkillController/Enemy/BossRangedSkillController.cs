using System.Collections.Generic;
using System.Collections;
using Skills.Base;
using UnityEngine;

// 보스 스킬에서 떨어지는 태양 VFX의 움직임을 제어하는 컴포넌트.
public class BossRangedSkillController : FieldEffectController
{
    private BossExplosionSkill skillData;

    private float castTime; // 보스가 스킬을 시전하는 시간
    private float fallDuration; // 해 파티클 낙하 시간
    private float lingeringDuration; // 폭발 후 틱 대미지 효과 지속 시간 

    private void Awake()
    {

    }

    // 메서드 오버로딩 : 부모의 Initialize에 없는 인자를 추가해서 사용하는 개념임
    // 그래서 얘는 확장 개념이고, 오버라이드는 대체 개념이다.
    public void Initialize(
        UnitEntity caster,
        IReadOnlyCollection<Vector2Int> skillRangeGridPositions,
        float fieldDuration,
        float tickDamageRatio,
        float interval,
        float castTime,
        float fallDuration,
        float lingeringDuration,
        GameObject hitEffectPrefab,
        string hitEffectTag)
    {
        base.Initialize(caster, skillRangeGridPositions, fieldDuration, tickDamageRatio, interval, hitEffectPrefab, hitEffectTag);

        this.castTime = castTime;
        this.fallDuration = fallDuration;
        this.lingeringDuration = lingeringDuration;

        StartCoroutine(PlaySkillCoroutine());

        // 일단 캐스터가 죽어도 이펙트는 남는 게 맞다고 생각해서 
        // 사망 이벤트가 발생해도 제거하진 않겠음
    }

    private IEnumerator PlaySkillCoroutine()
    {
        // 1. 해 파티클 목표 위치에 떨어지는 효과 실행

        yield return new WaitForSeconds(fallDuration); // 낙하시간 동안 대기

        // 2. 낙하 후에 폭발 이펙트 실행
        // 폭발 시 범위 타일들에 임팩트 대미지를 주고 지속 대미지를 입히는 필드가 남음

        yield return new WaitForSeconds(lingeringDuration); // 필드 지속시간 동안 대기



    }

    protected override void ApplyInitialEffect(UnitEntity target)
    {
        throw new System.NotImplementedException();
    }

    protected override void ApplyPeriodicEffect()
    {
        throw new System.NotImplementedException();
    }

    protected override void CheckTargetsInField()
    {
        throw new System.NotImplementedException();
    }
}
