public class StunEffect: CrowdControl
{
    private float originalSpeed;
    private bool wasStunned = false;



    public override void Initialize(ICrowdControlTarget target, UnitEntity Source, float duration)
    {
        base.Initialize(target, Source, duration);
    }

    protected override void ApplyEffect()
    {
        if (target != null)
        {
            isActive = true;
            originalSpeed = target.MovementSpeed;
            target.SetMovementSpeed(0f);
            wasStunned = true;
        }
    }

    protected override void RemoveEffect()
    {
        if (target != null && wasStunned)
        {
            target.SetMovementSpeed(originalSpeed);
            wasStunned = false;
            isActive = false;
        }
    }
}