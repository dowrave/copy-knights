using UnityEngine;

public abstract class CrowdControl
{
    protected Enemy target;
    protected UnitEntity source;
    protected float duration;
    protected float elapsedTime;
    protected bool isActive = false;

    public bool IsExpired => elapsedTime >= duration;

    // CC 효과 초기화 및 적용
    public virtual void Initialize(Enemy target, UnitEntity source, float duration)
    {
        this.target = target;
        this.source = source;
        this.duration = duration;
        this.elapsedTime = 0f;

        ApplyEffect();
    }

    // Monobehaviour 상속이 아니므로 그냥 Update라는 이름만 갖고 있는 개념임
    public virtual void Update()
    {
        if (!isActive) return;

        elapsedTime += Time.deltaTime;

        if (IsExpired)
        {
            RemoveEffect();
        }
    }

    public virtual void ForceRemove()
    {
        if (isActive)
        {
            RemoveEffect();
        }
    }
    protected abstract void ApplyEffect();
    protected abstract void RemoveEffect();
}
