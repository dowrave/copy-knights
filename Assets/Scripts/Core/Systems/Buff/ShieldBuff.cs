using UnityEngine;
using UnityEngine.VFX;

public class ShieldBuff : Buff
{
    private float shieldAmount;
    private GameObject shieldEffectPrefab;
    private GameObject currentShieldEffect;

    // 재진입 방지 플래그
    private bool isRemoving = false; 

    public ShieldBuff(float amount, float duration)
    {
        this.shieldAmount = amount;
        this.duration = duration;
        this.buffName = "Shield";
    }

    public override void OnApply(UnitEntity owner, UnitEntity caster)
    {
        base.OnApply(owner, caster);
        if (owner is Operator op)
        {
            op.shieldSystem.OnShieldChanged += HandleShieldChanged;
            op.ActivateShield(shieldAmount);
        }
    }

    public override void OnRemove()
    {
        // 재진입 방지 패턴 1 : 플래그
        if (isRemoving) return;
        isRemoving = true;

        if (owner is Operator op)
        {
            // 재진입 방지 패턴 2 : 이벤트 구독 먼저 해제
            // DeactivateShield로 인한 이벤트를 받지 않도록 연결을 끊음
            op.shieldSystem.OnShieldChanged -= HandleShieldChanged;
            op.DeactivateShield();
        }
        
        base.OnRemove();
    }

    private void HandleShieldChanged(float currentShield, bool isShieldDepleted)
    {
        if (isShieldDepleted && owner != null && !isRemoving)
        {
            owner.RemoveBuff(this);
        }
    }
}

