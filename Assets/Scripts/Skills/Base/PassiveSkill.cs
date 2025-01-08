// Skills/Base/PassiveSkill.cs
using UnityEngine;

namespace Skills.Base
{
    public abstract class PassiveSkill : BaseSkill
    {
        // 공격 시 호출되는 메서드 - damage와 showDamage를 수정할 수 있음
        public virtual void OnAttack(Operator op, ref float damage, ref bool showDamage) { }

        // PassiveSkill은 직접 Activate를 호출하지 않음
        public override void Activate(Operator op)
        {
            Debug.LogWarning($"Passive skill [{skillName}] cannot be activated directly");
        }
    }
}