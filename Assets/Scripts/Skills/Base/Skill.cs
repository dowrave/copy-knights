using System.Collections;
using UnityEngine;

namespace Skills.Base
{
    public abstract class Skill : ScriptableObject
    {
        public string Name;
        [TextArea(3, 10)]
        public string description;
        public float SPCost;
        public Sprite SkillIcon;

        public virtual bool AutoRecover { get; } = true; // 아무것도 하지 않아도 SP 자동 회복(<=> 공격 시 SP 회복)
        public virtual bool AutoActivate { get; } = false; // 자동발동 여부
        public virtual bool ModifiesAttackAction { get; } = false; // 기본 공격이 다르게 나가는 스킬일 때 true

        // 수동으로 발동
        public abstract void Activate(Operator op);

        // 공격 시에 적용
        public virtual void OnAttack(Operator op, ref float damage, ref bool showDamage) { } 

        public virtual void PerformSkillAction(Operator op) { }

        protected virtual IEnumerator HandleSkillDuration(Operator op, float duration)
        {
            op.StartSkillDurationDisplay(duration);

            // 버프 지속 시간 동안의 동작
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                yield return null;
                elapsedTime += Time.deltaTime;
                op.UpdateSkillDurationDisplay(1 - (elapsedTime / duration));
            }

            op.EndSkillDurationDisplay();
        }

        // 스킬 종료시의 처리
        protected virtual void OnSkillEnd(Operator op) { }

    }

}