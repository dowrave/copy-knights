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

        protected override void SetDefaults()
        {
            autoRecover = true;
            modifiesAttackAction = true;
        }

        public override void Activate(Operator op)
        {
            if (!op.IsDeployed || !op.CanUseSkill()) return;

            op.SetAttackCooldown(0f);

            base.PlaySkillEffect(op);
            PlayAdditionalEffects(op);
            
            if (duration > 0 )
            {
                op.StartCoroutine(HandleSkillDuration(op));
            }
        }

        public override void PerformChangedAttackAction(Operator op)
        {
            if (!op.IsSkillOn) return;
            op.StartCoroutine(PerformDoubleShot(op));
        }

        private IEnumerator PerformDoubleShot(Operator op)
        {
            UnitEntity target = op.CurrentTarget;
            if (target == null) yield break;

            float modifiedDamage = op.AttackPower * damageMultiplier;

            // 공격 1
            op.Attack(target, modifiedDamage);
            yield return new WaitForSeconds(delayBetweenShots);

            // 공격 2 - 타겟 생전 확인
            if (target != null && !target.Equals(null))
            {
                op.Attack(target, modifiedDamage);
            }
        }
    }
}