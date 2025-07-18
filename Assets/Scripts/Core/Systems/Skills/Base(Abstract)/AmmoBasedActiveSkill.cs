using UnityEngine;
using System.Collections;
using Skills.OperatorSkills;

namespace Skills.Base
{
    // źȯ ���� ���ѵǾ� �ְ� ���ӽð��� ������ ��ų�� �߻� Ŭ����
    public abstract class AmmoBasedActiveSkill : ActiveSkill
    {
        [Header("Ammo Settings")]
        [SerializeField] protected int maxAmmo;

        [Header("Attack Modifications")]
        [SerializeField] StatModifierSkill.StatModifiers statModifier;

        public override void Activate(Operator op)
        {
            CommonActivation(op);

            // ���� ���� ���� ����
            StatModificationBuff statBuff = new StatModificationBuff(float.PositiveInfinity, statModifier);

            // ���� Ƚ�� ���� ���� ����
            AttackCounterBuff attackCounterBuff = new AttackCounterBuff(maxAmmo);

            // ���� Ƚ�� ���� ������ �ٸ� ���� ����
            attackCounterBuff.LinkBuff(statBuff);

            // ���� Ƚ�� ���� ���� ���� �� ����� �Լ� ����
            // OnSkillEnd�� Operator�� �ް� �ݹ��� ���ڰ� �����Ƿ� ���ٽ����� ���μ� ȣ���Ѵ�.
            attackCounterBuff.OnRemovedCallback += () => OnSkillEnd(op);

            // ���� ���� �߰�
            op.AddBuff(statBuff);
            op.AddBuff(attackCounterBuff);

            // ���ӽð��� ������ ��ų�̱� ������ �̷� ������ ��������. ���� �ִٸ� �θ� Ŭ������ ������ �� ����.
        }

        public virtual void TerminateSkill(Operator op)
        {
            OnSkillEnd(op);
        }

    }
}
