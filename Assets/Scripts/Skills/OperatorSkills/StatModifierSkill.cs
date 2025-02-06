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
            public Vector2Int[] attackRangeModifier; // �������� ������ ���� ���� ������ �״�� �̿���
        }

        [Header("Stat Modification Settings")]
        [SerializeField] private StatModifiers modifiers = new StatModifiers();
        
        // �� �ӽ� ���� �ʵ�
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
            // ���� ü���� ������ �����ϸ鼭 �ִ� ü���� �����մϴ�
            float healthRatio = op.CurrentHealth / op.MaxHealth;
            op.MaxHealth *= modifiers.healthModifier;
            op.CurrentHealth = op.MaxHealth * healthRatio;

            // �ٸ� ���ȵ��� �����մϴ�
            op.AttackPower *= modifiers.attackPowerModifier;
            op.AttackSpeed *= modifiers.attackSpeedModifier;
            op.Defense *= modifiers.defenseModifier;
            op.MagicResistance *= modifiers.magicResistanceModifier;

            // ��� ���� �����մϴ� (����ġ�� ������ ��쿡��)
            if (modifiers.blockCountModifier.HasValue)
            {
                op.MaxBlockableEnemies = modifiers.blockCountModifier.Value;
            }

            // ���� ������ �����մϴ�
            if (modifiers.attackRangeModifier != null && modifiers.attackRangeModifier.Length > 0)
            {
                UpdateAttackRange(op);
            }
        }

        // ���� ����
        private void RestoreOriginalStats(Operator op)
        {
            // ���� ü���� ���� �ִ� ü�º��� ���ٸ�, ���� �ִ� ü������ �����մϴ�
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