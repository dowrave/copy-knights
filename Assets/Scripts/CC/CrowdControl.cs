using UnityEngine;
using UnityEngine.VFX;

public abstract class CrowdControl
{
    protected ICrowdControlTarget? target; // CC ������
    protected UnitEntity? source; // CC ������
    protected float duration;
    protected float elapsedTime;
    protected bool isActive = false;

    private GameObject? CCVFX;
    private ParticleSystem? vfxPs;
    private VisualEffect? vfxGraph;

    public bool IsExpired => elapsedTime >= duration || target == null;

    // CC ȿ�� �ʱ�ȭ �� ����
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

    // Monobehaviour ����� �ƴϹǷ� �׳� Update��� �̸��� ���� �ִ� ������
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
            CCVFX = CCEffectManager.Instance.CreateCCVFXObject(this, mb.transform); // �ڽ� Ŭ�������� this�� ȣ��� ��, �ڽ� Ŭ������ type�� ��

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


