using System.Collections.Generic;
using UnityEngine;


namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Stat Modifier Skill", menuName = "Skills/Stat Modifier With OnHit Skill")]
    public class StatModifierWithOnHitSkill : StatModifierSkill
    {
        [Header("On-Hit Effect Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float stunChance = 0f;
        [SerializeField] private float stunDuration = 1.0f;

        // 스킬이 부여하는 버프를 저장하는 필드
        private StunOnHitBuff stunBuffInstance;

        public override void OnSkillActivated(Operator op)
        {
            // 스탯 강화 효과
            base.OnSkillActivated(op);

            // 스턴 효과 추가
            if (stunChance > 0)
            {
                stunBuffInstance = new StunOnHitBuff(duration, stunChance, stunDuration);
                op.AddBuff(stunBuffInstance); // 버프를 추가하면 Buff.OnAfterAttack에 의해 기절 효과가 묻어나감
            }
        }

        public override void OnSkillEnd(Operator op)
        {
            op.RemoveBuff(stunBuffInstance);
            stunBuffInstance = null;
            
            base.OnSkillEnd(op);
        }
    }
}
