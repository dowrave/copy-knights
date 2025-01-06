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

        public virtual bool AutoRecover { get; } = true; // �ƹ��͵� ���� �ʾƵ� SP �ڵ� ȸ��(<=> ���� �� SP ȸ��)
        public virtual bool AutoActivate { get; } = false; // �ڵ��ߵ� ����
        public virtual bool ModifiesAttackAction { get; } = false; // �⺻ ������ �ٸ��� ������ ��ų�� �� true

        // �������� �ߵ�
        public abstract void Activate(Operator op);

        // ���� �ÿ� ����
        public virtual void OnAttack(Operator op, ref float damage, ref bool showDamage) { } 

        public virtual void PerformSkillAction(Operator op) { }

        protected virtual IEnumerator HandleSkillDuration(Operator op, float duration)
        {
            op.StartSkillDurationDisplay(duration);

            // ���� ���� �ð� ������ ����
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                yield return null;
                elapsedTime += Time.deltaTime;
                op.UpdateSkillDurationDisplay(1 - (elapsedTime / duration));
            }

            op.EndSkillDurationDisplay();
        }

        // ��ų ������� ó��
        protected virtual void OnSkillEnd(Operator op) { }

    }

}