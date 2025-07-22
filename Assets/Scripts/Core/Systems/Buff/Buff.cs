using UnityEngine;
using UnityEngine.VFX;
using Skills.Base;
using System.Collections.Generic;
using System.Linq;

// 스킬의 부속품이 되는 버프 시스템의 기초
public abstract class Buff
{
    public string buffName;
    public float duration; // 버프의 지속 시간. 무한이면 float.PositiveInfinity
    protected float elapsedTime; // 지속 시간 체크용
    public UnitEntity owner; // 버프 적용 대상
    public UnitEntity caster; // 버프 시전자

    public virtual bool IsDebuff => false; // 버프 종류 구분하는 프로퍼티

    public virtual bool ModifiesAttackAction => false; // 공격 방법이 바뀌는가?

    protected GameObject? vfxInstance;
    protected ParticleSystem? vfxParticleSystem;
    protected VisualEffect? vfxGraph;

    public System.Action OnRemovedCallback; // 버프 종료 시에 호출되는 콜백 함수

    protected List<Buff> linkedBuffs = new List<Buff>(); // 연결된 버프들. 같이 관리되기를 원하는 버프가 있다면

    // 공격 이펙트 오버라이드 정보
    public GameObject MeleeAttackEffectOverride { get; protected set; }
    public BaseSkill SourceSkill { get; protected set; }

    // owner.AddBuff에서 실행됨
    public virtual void OnApply(UnitEntity owner, UnitEntity caster)
    {
        this.owner = owner;
        this.caster = caster;
        elapsedTime = 0f;

        // 적용될 때 VFX 재생
        PlayVFX();
    }

    // 버프 제거 시에 호출
    // UnitEntity.RemoveBuff : 버프 리스트에서 버프를 제거한 뒤 OnRemove가 동작하는 방식
    public virtual void OnRemove()
    {
        // 연결된 버프들이 있다면 우선 제거함
        foreach (var buff in linkedBuffs.ToList())
        {
            owner.RemoveBuff(buff);
        }

        // 스킬의 후처리 콜백 호출
        OnRemovedCallback?.Invoke();
        RemoveVFX();
    }

    // 매 프레임마다 업데이트가 필요하면 호출
    public virtual void OnUpdate()
    {
        if (duration != float.PositiveInfinity)
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= duration)
            {
                owner.RemoveBuff(this);
            }
        }
    }


    public virtual void OnBeforeAttack(UnitEntity owner, ref float damage, ref AttackType attackType, ref bool showDamagePopup) { } // 공격 전 호출
    public virtual void OnAfterAttack(UnitEntity owner, UnitEntity target) { } // 공격 후 호출
    public virtual void PerformChangedAttackAction(UnitEntity owner)
    {
        if (owner is Operator op)
        {
            op.SetAttackDuration();
            op.SetAttackCooldown();
        }
    }

    // 스킬로부터 받은 VFX 오버라이드 정보를 넣는다
    public virtual void SetAttackVFXOverrides(BaseSkill sourceSkill)
    {
        if (sourceSkill == null) return;

        this.SourceSkill = sourceSkill;
        this.MeleeAttackEffectOverride = sourceSkill.meleeAttackEffectOverride;
    }

    // 버프에 포함된 VFX 이펙트를 재생한다.
    // 일단은 스턴 같은 효과에만 사용된다. 스킬 지속시간 같은 상황은 별도.
    protected virtual void PlayVFX()
    {
        if (BuffEffectManager.Instance != null && owner != null)
        {
            vfxInstance = BuffEffectManager.Instance.CreateBuffVFXObject(this, owner.transform);

            if (vfxInstance != null)
            {
                vfxParticleSystem = vfxInstance.GetComponent<ParticleSystem>();
                if (vfxParticleSystem != null)
                {
                    vfxParticleSystem.Play();
                    return;
                }

                vfxGraph = vfxInstance.GetComponent<VisualEffect>();
                if (vfxGraph != null)
                {
                    vfxGraph.Play();
                    return;
                }
            }
        }

    }

    protected virtual void RemoveVFX()
    {
        if (vfxParticleSystem != null)
        {
            vfxParticleSystem.Stop();
        }
        if (vfxGraph != null)
        {
            vfxGraph.Stop();
        }

        GameObject.Destroy(vfxInstance);

        vfxInstance = null;
        vfxParticleSystem = null;
        vfxGraph = null;
    }

    // 이 버프가 해제될 때 함께 해제되는 버프를 여기에 포함시킨다.
    // 다른 버프들을 다 
    public void LinkBuff(Buff buffToLink)
    {
        if (buffToLink != null)
        {
            linkedBuffs.Add(buffToLink);
        }
    }

}