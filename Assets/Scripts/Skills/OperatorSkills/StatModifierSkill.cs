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
            public Vector2Int[] attackRangeModifier; // 설정되지 않으면 원본 공격 범위를 그대로 이용함
        }

        [Header("Stat Modification Settings")]
        [SerializeField] private StatModifiers modifiers = new StatModifiers();
        
        // 값 임시 저장 필드
        private float originalMaxHealth;
        private float originalAttackPower;
        private float originalAttackSpeed;
        private float originalDefense;
        private float originalMagicResistance;
        int originalBlockableEnemies;
        List<Vector2Int> originalAttackableTiles;

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
            SafeDestroySkillVFX(VfxInstance);
            base.OnSkillEnd(op);
        }

        private void StoreOriginalStats(Operator op)
        {
            originalMaxHealth = op.MaxHealth;
            originalAttackPower = op.AttackPower;
            originalAttackSpeed = op.AttackSpeed;
            originalDefense = op.currentStats.Defense;
            originalMagicResistance = op.currentStats.MagicResistance;
            originalBlockableEnemies = op.currentStats.MaxBlockableEnemies;
            originalAttackableTiles = new List<Vector2Int>(op.CurrentAttackableTiles);
        }

        private void ApplyStatModifiers(Operator op)
        {
            // 현재 체력의 비율을 유지하면서 최대 체력을 수정합니다
            float healthRatio = op.CurrentHealth / op.MaxHealth;
            op.MaxHealth *= modifiers.healthModifier;
            op.CurrentHealth = op.MaxHealth * healthRatio;

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
            if (modifiers.attackRangeModifier != null && modifiers.attackRangeModifier.Length > 0)
            {
                UpdateAttackRange(op);
            }
        }

        // 스탯 복구
        private void RestoreOriginalStats(Operator op)
        {
            // 현재 체력이 원래 최대 체력보다 높다면, 원래 최대 체력으로 제한합니다
            op.MaxHealth = originalMaxHealth;
            if (op.CurrentHealth > originalMaxHealth)
            {
                op.CurrentHealth = originalMaxHealth;
            }

            op.AttackPower = originalAttackPower;
            op.AttackSpeed = originalAttackSpeed;
            op.Defense = originalDefense;
            op.MagicResistance = originalMagicResistance;
            op.MaxBlockableEnemies = originalBlockableEnemies;
            op.CurrentAttackableTiles = originalAttackableTiles;
        }

        private void UpdateAttackRange(Operator op)
        {
            
            List<Vector2Int> newRange = new List<Vector2Int>(op.CurrentAttackableTiles);

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

            op.CurrentAttackableTiles = newRange;
        }
    }
}