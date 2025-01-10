using UnityEngine;

public abstract class CrowdControl
{
    protected Enemy target;
    protected UnitEntity source;
    protected float duration;
    protected float elapsedTime;
    protected bool isActive = false;

    public bool IsExpired => elapsedTime >= duration;

    // CC ȿ�� �ʱ�ȭ �� ����
    public virtual void Initialize(Enemy target, UnitEntity source, float duration)
    {
        this.target = target;
        this.source = source;
        this.duration = duration;
        this.elapsedTime = 0f;

        ApplyEffect();
    }

    // Monobehaviour ����� �ƴϹǷ� �׳� Update��� �̸��� ���� �ִ� ������
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
