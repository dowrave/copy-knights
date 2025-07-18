using UnityEngine;
using Skills.Base;

using System.Collections.Generic;
using UnityEngine.VFX;


namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Stat Modifier Skill", menuName = "Skills/Stat Modifier Skill")]
    public class StatModifierSkill: ActiveSkill
    {
        // Buff�� ���� �ʰ� Skill�� �� : ������ ������ �δ� �� ����.(Buff�� ���� ���� ����)
        [System.Serializable] 
        public class StatModifiers
        {
            public float healthModifier = 1f;
            public float attackPowerModifier = 1f;
            public float attackSpeedModifier = 1f;
            public float defenseModifier = 1f;
            public float magicResistanceModifier = 1f;
            public int? blockCountModifier = null;
            public AttackType attackType;
            public List<Vector2Int> attackRangeModifier = new List<Vector2Int>();
        }

        [Header("Stat Modification Settings")]
        [SerializeField] private StatModifiers modifiers = new StatModifiers();

        protected StatModificationBuff statBuffInstance;


        protected override void SetDefaults()
        {
            autoRecover = true;
        }

        protected override void PlaySkillEffect(Operator op)
        {
            // ���� ��ȭ ���� ���� �� ����
            statBuffInstance = new StatModificationBuff(duration, modifiers);
            op.AddBuff(statBuffInstance);
        }

        protected override void OnSkillEnd(Operator op)
        {
            if (statBuffInstance != null)
            {
                op.RemoveBuff(statBuffInstance);
                statBuffInstance = null;
            }
            
            base.OnSkillEnd(op);
        }

    }
}