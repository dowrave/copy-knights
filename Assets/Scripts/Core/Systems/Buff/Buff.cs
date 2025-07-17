using UnityEngine;
using UnityEngine.VFX;

// 스킬의 부속품이 되는 버프 시스템의 기초
public abstract class Buff
{
    public string buffName;
    public float duration; // 버프의 지속 시간. 무한이면 float.PositiveInfinity
    protected float elapsedTime; // 지속 시간 체크용
    public UnitEntity owner; // 버프 적용 대상
    public UnitEntity caster; // 버프 시전자

    public virtual bool IsDebuff => false; // 버프 종류 구분하는 프로퍼티

    protected GameObject? vfxInstance;
    protected ParticleSystem? vfxParticleSystem;
    protected VisualEffect? vfxGraph;

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
    public virtual void OnRemove()
    {
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

}