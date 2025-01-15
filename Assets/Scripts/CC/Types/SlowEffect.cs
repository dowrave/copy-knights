using UnityEngine;

public class SlowEffect : CrowdControl
{
    private float slowAmount;
    private float originalSpeed;

    public void Initialize(ICrowdControlTarget target, UnitEntity source, float duration, float slowAmount)
    {
        this.slowAmount = Mathf.Clamp01(slowAmount);
        base.Initialize(target, source, duration);
    }

    protected override void ApplyEffect()
    {
        if (target != null)
        {
            isActive = true;
            originalSpeed = target.MovementSpeed;
            target.SetMovementSpeed(originalSpeed * (1f - slowAmount));
        }
    }

    protected override void RemoveEffect()
    {
        if (target != null)
        {
            target.SetMovementSpeed(originalSpeed);
            isActive = false;
        }
    }
}
