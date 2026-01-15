using UnityEngine;

public class SlowBuff : Buff
{
    public override bool IsDebuff => true;
    private float slowAmount; 

    public SlowBuff(float duration, float slowAmount)
    {
        this.buffName = "Slow";
        this.duration = duration;
        this.slowAmount = slowAmount;
    }

    public override void OnApply(UnitEntity owner, UnitEntity caster)
    {
        base.OnApply(owner, caster);
        owner.AddStatModifier(StatType.MovementSpeed, -slowAmount); 
    }

    public override void OnRemove()
    {
        // owner.SetMovementSpeed(originalSpeed);
        owner.RemoveStatModifier(StatType.MovementSpeed, -slowAmount);
        base.OnRemove();
    }
}
