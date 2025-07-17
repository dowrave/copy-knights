using UnityEngine;
using Skills.Base;

using System.Collections.Generic;
using UnityEngine.VFX;


namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Stat Modifier Skill", menuName = "Skills/Stat Modifier Skill")]
    public class StatModifierSkill: ActiveSkill
    {
        // Buff에 두지 않고 Skill에 둠 : 데이터 영역에 두는 게 낫다.(Buff는 실제 실행 영역)
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
            // 스탯 강화 버프 생성 및 적용
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