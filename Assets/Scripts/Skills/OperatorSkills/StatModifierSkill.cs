using UnityEngine;
using Skills.Base;

using System.Collections.Generic;
using UnityEngine.VFX;


namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Stat Modifier Skill", menuName = "Skills/Stat Modifier Skill")]
    public class StatModifierSkill: ActiveSkill
    {
        [System.Serializable] 
        public class StatModifiers
        {
            public float healthModifier = 1f;
            public float attackPowerModifier = 1f;
            public float attackSpeedModifier = 1f;
            public float defenseModifier = 1f;
            public float magicResistanceModifier = 1f;

            public int? blockCountModifier = null;
            public List<Vector2Int> attackRangeModifier = new List<Vector2Int>();
        }

        [Header("Stat Modification Settings")]
        [SerializeField] private StatModifiers modifiers = new StatModifiers();
        
        // 값 임시 저장 필드
        private float originalMaxHealth;
        private float originalAttackPower;
        private float originalAttackSpeed;
        private float originalDefense;
        private float originalMagicResistance;
        private int originalBlockableEnemies;
        List<Vector2Int> originalAttackableGridPos = new List<Vector2Int>();

        protected override void SetDefaults()
        {
            autoRecover = true;
        }

        protected override void PlaySkillEffect(Operator op)
        {
            StoreOriginalStats(op);
            ApplyStatModifiers(op);
        }

        protected override void OnSkillEnd(Operator op)
        {
            RestoreOriginalStats(op);
            if (VfxInstance != null)
            {
                SafeDestroySkillVFX(VfxInstance);
            }
            base.OnSkillEnd(op);
        }

        private void StoreOriginalStats(Operator op)
        {
            originalMaxHealth = op.MaxHealth;
            originalAttackPower = op.AttackPower;
            originalAttackSpeed = op.AttackSpeed;
            originalDefense = op.currentOperatorStats.Defense;
            originalMagicResistance = op.currentOperatorStats.MagicResistance;
            originalBlockableEnemies = op.currentOperatorStats.MaxBlockableEnemies;
            originalAttackableGridPos = new List<Vector2Int>(op.CurrentAttackableGridPos);
        }

        private void ApplyStatModifiers(Operator op)
        {
            // 현재 체력의 비율을 유지하면서 최대 체력을 수정합니다
            float healthRatio = op.CurrentHealth / op.MaxHealth;
            op.ChangeMaxHealth(op.MaxHealth * modifiers.healthModifier);

            // MaxHealth가 맞음 : 현재 체력 = 변한 최대 체력 * 기존 비율
            op.ChangeCurrentHealth(op.MaxHealth * healthRatio); 

            // 다른 스탯들을 수정합니다
            op.AttackPower *= modifiers.attackPowerModifier;
            op.AttackSpeed *= modifiers.attackSpeedModifier;
            op.Defense *= modifiers.defenseModifier;
            op.MagicResistance *= modifiers.magicResistanceModifier;

            // 블록 수를 수정합니다 (수정치가 지정된 경우에만)
            if (modifiers.blockCountModifier.HasValue)
            {
                op.MaxBlockableEnemies = modifiers.blockCountModifier.Value;
            }

            // 공격 범위를 수정합니다
            if (modifiers.attackRangeModifier != null && modifiers.attackRangeModifier.Count > 0)
            {
                UpdateAttackRange(op);
            }
        }

        // 스탯 복구
        private void RestoreOriginalStats(Operator op)
        {
            RestoreOriginalHealth(op);

            op.AttackPower = originalAttackPower;
            op.AttackSpeed = originalAttackSpeed;
            op.Defense = originalDefense;
            op.MagicResistance = originalMagicResistance;
            op.MaxBlockableEnemies = originalBlockableEnemies;
            op.CurrentAttackableGridPos = originalAttackableGridPos;
        }

        private void UpdateAttackRange(Operator op)
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

        private void RestoreOriginalHealth(Operator op)
        {
            op.ChangeMaxHealth(originalMaxHealth);

            // 1. 스탯 상승 시의 현재 체력이 원상 복귀 후 최대 체력보다 높으면 최대 체력 보정
            if (op.CurrentHealth > originalMaxHealth)
            {
                op.ChangeCurrentHealth(originalMaxHealth);
            }

            // 2. 그렇지 않은 경우는 비율에 맞춰 돌아오는 게 아니라, 버프가 걸린 상태의 현재 체력 유지
        }
    }
}