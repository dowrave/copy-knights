using System.Collections;
using Skills.Base;
using UnityEngine;
using UnityEngine.VFX;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Double Shot Skill", menuName = "Skills/Double Shot Skill")]
    public class DoubleShotSkill: ActiveSkill
    {
        [Header("Skill Settings")]
        [SerializeField] private float damageMultiplier = 1.2f;
        [SerializeField] private float delayBetweenShots = 0.1f;

        private DoubleShotBuff? _doubleShotBuff;

        protected override void SetDefaults()
        {
            autoRecover = true;
        }

        public override void OnSkillActivated(Operator op)
        {
            // 스킬이 시작될 때 적용되는 효과
            _doubleShotBuff = new DoubleShotBuff(delayBetweenShots, damageMultiplier);
            op.AddBuff(_doubleShotBuff);
        }

        public override void OnSkillEnd(Operator op)
        {
            if (_doubleShotBuff != null)
            {
                op.RemoveBuff(_doubleShotBuff);
                _doubleShotBuff = null;
            }

            base.OnSkillEnd(op);
        }
    }
}