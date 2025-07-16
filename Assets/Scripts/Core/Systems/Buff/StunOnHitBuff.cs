using UnityEngine;

public class StunOnHitBuff : Buff
{
    private float stunChance;
    private float stunDuration;

    public StunOnHitBuff(float chance, float duration)
    {
        buffName = "Stun On Hit";
        stunChance = chance;
        stunDuration = duration;
    }

    public override void OnAfterAttack(UnitEntity target)
    {
        if (Random.value <= stunChance)
        {
            if (target != null && target.CurrentHealth > 0)
            {
                StunEffect stunEffect = new StunEffect();
                stunEffect.Initialize(target, owner, stunDuration);
                target.AddCrowdControl(stunEffect);
            }
        }
    }
}