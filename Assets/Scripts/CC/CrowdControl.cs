using UnityEngine;
using UnityEngine.VFX;

public abstract class CrowdControl
{
    protected ICrowdControlTarget? target; // CC 피해자
    protected UnitEntity? source; // CC 가해자
    protected float duration;
    protected float elapsedTime;
    protected bool isActive = false;

    private GameObject? CCVFX;
    private ParticleSystem? vfxPs;
    private VisualEffect? vfxGraph;

    public bool IsExpired => elapsedTime >= duration || target == null;

    // CC 효과 초기화 및 적용
    public virtual void Initialize(ICrowdControlTarget? target, UnitEntity? source, float duration)
    {
        this.target = target;
        this.source = source;
        this.duration = duration;
        this.elapsedTime = 0f;

        ApplyEffect();
        PlayCCVFX();

        isActive = true;
    }

    // Monobehaviour 상속이 아니므로 그냥 Update라는 이름만 갖고 있는 개념임
    public virtual void Update()
    {
        if (!isActive) return;

        elapsedTime += Time.deltaTime;

        if (IsExpired)
        {
            RemoveCC();
        }
    }

    public virtual void ForceRemove()
    {
        if (isActive)
        {
            RemoveCC();
        }
    }

    protected virtual void RemoveCC()
    {
        RemoveVFX();
        RemoveEffect();
        isActive = false;
    }

    protected virtual void PlayCCVFX()
    {
        if (target is MonoBehaviour mb)
        {
            if (CCEffectManager.Instance == null) return;
            CCVFX = CCEffectManager.Instance.CreateCCVFXObject(this, mb.transform); // 자식 클래스에서 this가 호출될 때, 자식 클래스의 type이 들어감

            if (CCVFX != null)
            {
                vfxPs = CCVFX.GetComponent<ParticleSystem>();
                if (vfxPs != null)
                {
                    vfxPs.Play();
                    return;
                }

                vfxGraph = CCVFX.GetComponent<VisualEffect>();
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
        if (vfxPs != null)
        {
            vfxPs.Stop();
            vfxPs = null;
        }
        if (vfxGraph != null)
        {
            vfxGraph.Stop();
            vfxGraph = null;
        }

        GameObject.Destroy(CCVFX);
        CCVFX = null;
    }

    protected abstract void ApplyEffect();
    protected abstract void RemoveEffect();
}


