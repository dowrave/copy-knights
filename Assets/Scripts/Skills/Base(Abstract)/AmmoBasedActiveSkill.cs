using UnityEngine;
using System.Collections;

namespace Skills.Base
{
    // źȯ ���� ���ѵǾ� �ְ� ���ӽð��� ������ ��ų�� �߻� Ŭ����
    public abstract class AmmoBasedActiveSkill : ActiveSkill
    {
        [Header("Ammo Settings")]
        [SerializeField] protected int maxAmmo;

        [Header("Attack Modifications")]
        [SerializeField] protected float attackSpeedModifier = 1f;
        [SerializeField] protected float attackDamageModifier = 1f;

        protected int currentAmmo;

        protected float originalAttackSpeed;
        protected float originalAttackDamage;

        protected OperatorUI? operatorUI;

        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => maxAmmo;

        public override void Activate(Operator op)
        {
            caster = op;
            operatorUI = op.OperatorUI;

            if (!op.IsDeployed || !op.CanUseSkill()) return;

            PlaySkillVFX(op);
            PlayAdditionalVFX(op);

            currentAmmo = maxAmmo;

            originalAttackSpeed = op.AttackSpeed;
            originalAttackDamage = op.AttackPower;

            // ���� ���� ���� 
            op.AttackPower *= attackDamageModifier;
            op.AttackSpeed /= attackSpeedModifier;

            // ��ų ��� ���Ŀ��� ���� �ӵ� / ��� �ʱ�ȭ
            op.SetAttackDuration(0f);
            op.SetAttackCooldown(0f);

            // SPbar UI ������Ʈ
            operatorUI?.SwitchSPBarToAmmoMode(maxAmmo, currentAmmo);

            op.StartCoroutine(Co_HandleSkillDuration(op)); // �ڷ�ƾ�� MonoBehaviour������ ��� ������
        }

        public override void OnAfterAttack(Operator op)
        {
            // ��ų�� ������ ������ ����
            if (!op.IsSkillOn) return; 

            // ���� �� źȯ �Ҹ�
            ConsumeAmmo(op);

            // źȯ�� �����Ǹ� ��ų ����
            if (CurrentAmmo <= 0)
            {
                TerminateSkill(op);
            }
            
        }

        protected virtual void ConsumeAmmo(Operator op)
        {
            currentAmmo = Mathf.Max(0, currentAmmo - 1);

            // źȯ �Ҹ� �� UI ������Ʈ
            UpdateAmmoUI(op);
        }

        protected override void OnSkillEnd(Operator op)
        {
            // ��ų ���� �� ���� �������� ����
            op.AttackPower = originalAttackDamage;
            op.AttackSpeed = originalAttackSpeed;

            base.OnSkillEnd(op);
        }

        protected virtual void UpdateAmmoUI(Operator op)
        {
            op.OperatorUI?.UpdateAmmoDisplay(currentAmmo);
        }

        public virtual void TerminateSkill(Operator op)
        {
            OnSkillEnd(op);
            operatorUI?.SwitchSPBarToNormalMode();
        }

        public override IEnumerator Co_HandleSkillDuration(Operator op)
        {
            OnSkillStart(op);
            // op.StartSkillDurationDisplay(duration?��);
            PlaySkillEffect(op);

            while (op.IsSkillOn)
            {
                // ���۷����� �ı� �� ������ ����
                if (op == null) yield break; 

                // źȯ�� �������� ��ų ����
                if (currentAmmo <= 0)
                {
                    TerminateSkill(op); // ���� ������ �� �ȿ� ������
                    yield break;
                }

                // �⺻) ���� ����
                yield return null;
            }
        }
    }
}
