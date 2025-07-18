using System.Collections.Generic;
using UnityEngine;

public class AttackCounterBuff : Buff
{
    private int maxAttacks;
    private int currentAttacks;

    public int MaxAttacks => maxAttacks;
    public int CurrentAttacks => currentAttacks;

    private OperatorUI operatorUI;

    public System.Action<int, int> OnAmmoChanged;

    public AttackCounterBuff(int maxAttacks)
    {
        this.buffName = "Attack Counter";
        this.duration = float.PositiveInfinity;
        this.maxAttacks = maxAttacks;
    }

    public override void OnApply(UnitEntity owner, UnitEntity caster)
    {
        base.OnApply(owner, caster);
        currentAttacks = maxAttacks; // 최대 횟수를 정해놓고 1씩 줄이는 방식

        // 초기 탄환 상태 이벤트로 호출
        OnAmmoChanged?.Invoke(currentAttacks, maxAttacks);

        // UI를 탄환 모드로 전환
        if (owner is Operator op)
        {
            operatorUI = op.OperatorUI;
            operatorUI?.SwitchSPBarToAmmoMode(maxAttacks, currentAttacks);
        }
    }

    public override void OnAfterAttack(UnitEntity owner, UnitEntity target)
    {
        base.OnAfterAttack(owner, target);

        currentAttacks = Mathf.Max(0, currentAttacks - 1);
        
        OnAmmoChanged?.Invoke(currentAttacks, maxAttacks);
        // operatorUI?.UpdateAmmoDisplay(currentAttacks);

        if (currentAttacks <= 0)
        {
            // 연결된 모든 버프 제거
            foreach (var buff in linkedBuffs)
            {
                owner.RemoveBuff(buff);
            }

            owner.RemoveBuff(this);
        }
    }

    public override void OnRemove()
    {
        operatorUI?.SwitchSPBarToNormalMode();

        base.OnRemove();
    }
}