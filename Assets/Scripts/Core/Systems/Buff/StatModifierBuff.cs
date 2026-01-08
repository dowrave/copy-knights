using Skills.OperatorSkills;
using UnityEngine;
using System.Collections.Generic;

public class StatModificationBuff : Buff
{
    private StatModifierSkill.StatModifiers modifiers;

    // 원래 스탯 저장 필드
    // [StatController, BuffController 구현 중] 어떻게 처리할지 몰라 남겨둠
    private AttackType originalAttackType;
    private List<Vector2Int> originalAttackableGridPos;

    // 스킬에 명세된 내용을 받아서 실행시키는 방식
    public StatModificationBuff(float duration, StatModifierSkill.StatModifiers mods)
    {
        this.duration = duration;
        buffName = "Stat Boost";
        modifiers = mods;
    }

    public override void OnApply(UnitEntity owner, UnitEntity caster)
    {
        base.OnApply(owner, caster);

        // StatController 구현에 따른 변경된 로직
        // 예시) 공격력 50% 증가라면 값은 1.5가 입력되어 있음
        // modifier에는 0.5라는 값만 들어감
        // 만약 비슷한 로직이 중첩된다면 modifier는 합 연산으로 구현됨

        // 체력
        if (modifiers.healthModifier != 1.0f)
        {
            owner.AddStatModifier(StatType.MaxHP, modifiers.healthModifier);
        }

        // 방어력
        if (modifiers.defenseModifier != 1.0f)
        {
            owner.AddStatModifier(StatType.Defense, modifiers.defenseModifier);
        }

        // 마법저항력
        if (modifiers.magicResistanceModifier != 1.0f)
        {
            owner.AddStatModifier(StatType.MagicResistance, modifiers.magicResistanceModifier);
        }

        // 공격력
        if (modifiers.attackPowerModifier != 1.0f)
        {
            owner.AddStatModifier(StatType.AttackPower, modifiers.attackPowerModifier );
        }

        // 공격 속도  
        if (modifiers.attackSpeedModifier != 1.0f)
        {
            owner.AddStatModifier(StatType.AttackSpeed, modifiers.attackSpeedModifier);
        }

        // 저지 수는 덮어쓴다
        if (modifiers.blockCountModifier.HasValue)
        {
            float blockCountOverride = modifiers.blockCountModifier.Value;
            owner.AddStatOverride(StatType.MaxBlockCount, blockCountOverride);
        }

        // 아래는 일단 기존 로직 유지
        if (modifiers.attackType != AttackType.None)
        {
            originalAttackType = owner.AttackType;
            owner.AttackType = modifiers.attackType;
        }

        if (owner is Operator op)
        {
            // 공격 범위
            if (modifiers.attackRangeModifier != null && modifiers.attackRangeModifier.Count > 0)
            {
                originalAttackableGridPos = op.CurrentAttackableGridPos;
                
                List<Vector2Int> newRange = new List<Vector2Int>(op.CurrentAttackableGridPos);

                foreach (Vector2Int additionalTile in modifiers.attackRangeModifier)
                {
                    Vector2Int rotatedTile = PositionCalculationSystem.RotateGridOffset(
                        additionalTile,
                        op.FacingDirection.Value
                    );

                    if (!newRange.Contains(rotatedTile))
                    {
                        newRange.Add(rotatedTile);
                    }
                }
                op.CurrentAttackableGridPos = newRange;
            }
        }
    }

    // 버프 제거 시 호출
    public override void OnRemove()
    {
        // 체력
        if (modifiers.healthModifier != 1.0f)
        {
            owner.RemoveStatModifier(StatType.MaxHP, modifiers.healthModifier);

            // 이로 인해 변경된 최대 체력 및 현재 체력은 healthController에서 별도로 구현
        }

        // 방어력
        if (modifiers.defenseModifier != 1.0f)
        {
            owner.RemoveStatModifier(StatType.Defense, modifiers.defenseModifier);
        }

        // 마법저항력
        if (modifiers.magicResistanceModifier != 1.0f)
        {
            owner.RemoveStatModifier(StatType.MagicResistance, modifiers.magicResistanceModifier);
        }

        // 공격력
        if (modifiers.attackPowerModifier != 1.0f)
        {
            owner.RemoveStatModifier(StatType.AttackPower, modifiers.attackPowerModifier );
        }

        // 공격 속도  
        if (modifiers.attackSpeedModifier != 1.0f)
        {
            owner.RemoveStatModifier(StatType.AttackSpeed, modifiers.attackSpeedModifier);
        }

        // 저지 수
        if (modifiers.blockCountModifier.HasValue)
        {
            owner.RemoveStatOverride(StatType.MaxBlockCount);
        }

        owner.AttackType = originalAttackType;
        if (owner is Operator op)
        {
            op.CurrentAttackableGridPos = originalAttackableGridPos;
        }


        base.OnRemove();
    }
}