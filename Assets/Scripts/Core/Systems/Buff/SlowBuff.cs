using UnityEngine;

public class SlowBuff : Buff
{
    public override bool IsDebuff => true;

    private float slowMultiplier; //slowAmount만큼 느려짐 : 즉, 이 값은 (1f - SlowAmount)라는 배율임.

    public SlowBuff(float duration, float slowAmount)
    {
        this.buffName = "Slow";
        this.duration = duration;
        this.slowMultiplier = 1f - Mathf.Clamp01(slowAmount);
    }

    public override void OnApply(UnitEntity owner, UnitEntity caster)
    {
        base.OnApply(owner, caster);
        // originalSpeed = owner.MovementSpeed;
        // owner.SetMovementSpeed(originalSpeed * slowMultiplier);
        owner.AddStatModifier(StatType.MovementSpeed, slowMultiplier);
    }

    public override void OnRemove()
    {
        // owner.SetMovementSpeed(originalSpeed);
        owner.RemoveStatModifier(StatType.MovementSpeed, slowMultiplier);
        base.OnRemove();
    }
}
