using UnityEngine;
using Skills.Base;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Smash Skill", menuName = "Skills/Smash Skill")]
    public class SmashSkill: Skill
    {
        public float damageMultiplier = 2f;

        public override bool AutoRecover => false;
        public override bool AutoActivate => true;

        public override void Activate(Operator op)
        {
            
        }

        /// <summary>
        /// ��Ÿ ������ ������ Ȱ��ȭ ���� ���� ���ݿ� �����
        /// </summary>
        public override void OnAttack(Operator op, ref float damage, ref bool showDamage)
        {
            if (op.CurrentSP >= op.MaxSP) 
            {
                damage *= damageMultiplier;
                showDamage = true;
                op.CurrentSP = 0;
            }
            else
            {
                op.CurrentSP += 1;
                Debug.Log($"op.CurrentSP : {op.CurrentSP}");
            }
        }
    }
}