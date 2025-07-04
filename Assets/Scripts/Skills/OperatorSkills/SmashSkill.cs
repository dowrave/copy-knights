using UnityEngine;
using Skills.Base;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Smash Skill", menuName = "Skills/Smash Skill")]
    public class SmashSkill: PassiveSkill
    {
        [Header("Damage Settings")]
        public float damageMultiplier = 2f;

        protected override void SetDefaults()
        {
            autoActivate = true;
        }

        // 공격에 묻어나가는 로직
        public override void OnBeforeAttack(Operator op, ref float damage, ref bool showDamage)
        {
            if (op.CurrentSP >= op.MaxSP) 
            {
                damage *= damageMultiplier;
                showDamage = true;
                op.CurrentSP = 0;
            }
        }
    }
}