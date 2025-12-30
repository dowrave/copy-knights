using UnityEngine;

// "공격 시 일정 확률로 target을 기절시키는 버프". 즉 기절 자체를 구현하는 버프가 아니다.
public class StunOnHitBuff : Buff
{
    private float stunChance;
    private float stunDuration;

    public StunOnHitBuff(float duration, float stunChance, float stunDuration)
    {
        buffName = "Stun On Hit";
        this.duration = duration; // 버프 자체의 지속 시간
        this.stunChance = stunChance;
        this.stunDuration = stunDuration; // 버프로 인한 기절의 지속 시간
    }

    public override void OnAfterAttack(UnitEntity owner, UnitEntity target)
    {        
        if (Random.value <= stunChance) // x% 확률 = 랜덤값(0~1)이 확률보다 작다 로 구현
        {
            if (target != null && target.HealthSystem.CurrentHealth > 0)
            {
                StunBuff stunBuff = new StunBuff(stunDuration);
                target.AddBuff(stunBuff);
            }
        }
    }
}