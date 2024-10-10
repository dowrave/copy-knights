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

        // 수동으로 발동
        public abstract void Activate(Operator op);

        // 공격 시에 적용
        public virtual void OnAttack(Operator op, ref float damage, ref bool showDamage) { } 
    }

}