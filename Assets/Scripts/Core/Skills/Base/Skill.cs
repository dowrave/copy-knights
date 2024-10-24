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

        // �������� �ߵ�
        public abstract void Activate(Operator op);

        // ���� �ÿ� ����
        public virtual void OnAttack(Operator op, ref float damage, ref bool showDamage) { } 
    }

}