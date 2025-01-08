// Skills/Base/PassiveSkill.cs
using UnityEngine;

namespace Skills.Base
{
    public abstract class PassiveSkill : BaseSkill
    {
        // ���� �� ȣ��Ǵ� �޼��� - damage�� showDamage�� ������ �� ����
        public virtual void OnAttack(Operator op, ref float damage, ref bool showDamage) { }

        // PassiveSkill�� ���� Activate�� ȣ������ ����
        public override void Activate(Operator op)
        {
            Debug.LogWarning($"Passive skill [{skillName}] cannot be activated directly");
        }
    }
}