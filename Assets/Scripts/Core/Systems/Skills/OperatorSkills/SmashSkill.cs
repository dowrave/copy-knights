using UnityEngine;
using Skills.Base;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Smash Skill", menuName = "Skills/Smash Skill")]
    public class SmashSkill : PassiveSkill
    {
        [Header("Damage Settings")]
        public float damageMultiplier = 2f;
        public AttackType attackType = AttackType.Physical;

        protected override void SetDefaults()
        {
            autoActivate = true;
        }

        public override void OnUpdate(Operator op)
        {
            if (op.CurrentSP >= op.MaxSP && !op.HasBuff<SmashBuff>())
            {
                // 조건 만족 시 일회성 SmashBuff를 부여함
                SmashBuff buff = new SmashBuff(damageMultiplier, attackType, removeBuffAfterAttack: true);
                buff.SetAttackVFXOverrides(this);
                op.AddBuff(buff);
            }
        }
    }
}