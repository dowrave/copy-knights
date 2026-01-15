using UnityEngine;
using Skills.Base;

using System.Collections.Generic;
using UnityEngine.VFX;


namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Stat Modifier Skill", menuName = "Skills/Stat Modifier Skill")]
    public class StatModifierSkill: ActiveSkill
    {
        [Header("Stat Modification Settings")]
        [Tooltip("float modifier는 0이 기준값")]
        [SerializeField] private StatModifiers modifiers = new StatModifiers();

        protected StatModificationBuff statBuffInstance;

        protected override void SetDefaults()
        {
            autoRecover = true;
        }

        public override void OnActivated(Operator op)
        {
            // 스탯 강화 버프 생성 및 적용
            statBuffInstance = new StatModificationBuff(duration, modifiers);
            statBuffInstance.SetAttackVFXOverrides(this); // 변경되는 이펙트가 있다면 반영함
            op.AddBuff(statBuffInstance);
        }

        public override void OnEnd(Operator op)
        {
            if (statBuffInstance != null)
            {
                op.RemoveBuff(statBuffInstance);
                statBuffInstance = null;
            }
            
            base.OnEnd(op);
        }

        // Buff에 두지 않고 Skill에 둠 : 데이터 영역에 두는 게 낫다.(Buff는 실제 실행 영역)
        [System.Serializable] 
        public class StatModifiers
        {
            public float healthModifier = 0f;
            public float attackPowerModifier = 0f;
            public float attackSpeedModifier = 0f;
            public float defenseModifier = 0f;
            public float magicResistanceModifier = 0f;
            public int? blockCountModifier = null;
            public AttackType attackType;
            public List<Vector2Int> attackRangeModifier = new List<Vector2Int>();
        }

    }
}