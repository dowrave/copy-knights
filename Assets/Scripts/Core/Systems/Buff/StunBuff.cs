using UnityEngine;

public class StunBuff : Buff
{
    public override bool IsDebuff => true;

    public StunBuff(float duration)
    {
        buffName = "stun";
        this.duration = duration;
    }

    public override void OnApply(UnitEntity owner, UnitEntity caster)
    {
        base.OnApply(owner, caster);
        owner.AddRestriction(ActionRestriction.Stunned);
    }

    public override void OnRemove()
    {
        owner.RemoveRestriction(ActionRestriction.Stunned);
        base.OnRemove();
    }
}