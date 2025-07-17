using Skills.OperatorSkills;
using UnityEngine;
using System.Collections.Generic;

public class StatModificationBuff : Buff
{
    private StatModifierSkill.StatModifiers modifiers;

    // 원래 스탯 저장 필드
    private float originalMaxHealth;
    private float originalAttackPower;
    private float originalAttackSpeed;
    private float originalDefense;
    private float originalMagicResistance;
    private int originalBlockableEnemies;
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

        StoreOriginalStats(owner);
        ApplyStatModifiers(owner);
    }

    // 버프 제거 시 호출
    public override void OnRemove()
    {
        RestoreOriginalStats(owner);
        base.OnRemove();
    }

    private void StoreOriginalStats(UnitEntity owner)
    {
        Debug.Log($"owner : {owner}");
        Debug.Log($"owner.MaxHealth : {owner.MaxHealth}");
        originalMaxHealth = owner.MaxHealth;
        originalAttackPower = owner.AttackPower;
        originalAttackSpeed = owner.AttackSpeed;
        originalDefense = owner.Defense;
        originalMagicResistance = owner.MagicResistance;
        originalAttackType = owner.AttackType;
        if (owner is Operator op)
        {
            originalBlockableEnemies = op.MaxBlockableEnemies;
            originalAttackableGridPos = new List<Vector2Int>(op.CurrentAttackableGridPos);
        }
    }

    private void ApplyStatModifiers(UnitEntity owner) 
    {
        // 체력 수정 : 최대체력 증가에 맞춰 현재 체력도 비율로 증가
        float healthRatio = owner.CurrentHealth / owner.MaxHealth;
        owner.ChangeMaxHealth(owner.MaxHealth * modifiers.healthModifier);

        // MaxHealth가 맞음 : 현재 체력 = 변한 최대 체력 * 기존 비율
        owner.ChangeCurrentHealth(owner.MaxHealth * healthRatio); 

        // 스탯 수정
        owner.AttackPower *= modifiers.attackPowerModifier;
        owner.AttackSpeed /= modifiers.attackSpeedModifier; // 공격 속도 = 쿨다운이므로 줄어야 맞음
        owner.Defense *= modifiers.defenseModifier;
        owner.MagicResistance *= modifiers.magicResistanceModifier;
        owner.AttackType = modifiers.attackType;

        // 오퍼레이터 한정 변경 사항
        if (owner is Operator op)
        {
            // 저지 수
            if (modifiers.blockCountModifier.HasValue)
            {
                op.MaxBlockableEnemies = modifiers.blockCountModifier.Value;
            }

            // 공격 범위
            if (modifiers.attackRangeModifier != null && modifiers.attackRangeModifier.Count > 0)
            {
                List<Vector2Int> newRange = new List<Vector2Int>(op.CurrentAttackableGridPos);

                foreach (Vector2Int additionalTile in modifiers.attackRangeModifier)
                {
                    Vector2Int rotatedTile = DirectionSystem.RotateGridOffset(
                        additionalTile,
                        op.FacingDirection
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

    // 스탯 복구
    private void RestoreOriginalStats(UnitEntity owner)
    {
        RestoreOriginalHealth(owner);

        owner.AttackPower = originalAttackPower;
        owner.AttackSpeed = originalAttackSpeed;
        owner.Defense = originalDefense;
        owner.MagicResistance = originalMagicResistance;
        owner.AttackType = originalAttackType;

        if (owner is Operator op)
        {
            op.MaxBlockableEnemies = originalBlockableEnemies;
            op.CurrentAttackableGridPos = originalAttackableGridPos;
        }
    }

    private void RestoreOriginalHealth(UnitEntity owner)
    {
        owner.ChangeMaxHealth(originalMaxHealth);

        // 1. 스탯 상승 시의 현재 체력이 원상 복귀 후 최대 체력보다 높으면 최대 체력 보정
        if (owner.CurrentHealth > originalMaxHealth)
        {
            owner.ChangeCurrentHealth(originalMaxHealth);
        }

        // 2. 그렇지 않은 경우는 비율에 맞춰 돌아오는 게 아니라, 버프가 걸린 상태의 현재 체력 유지
    }
    
}