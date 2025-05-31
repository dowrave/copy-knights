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

        protected DeployableBarUI? deployableBarUI;

        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => maxAmmo;

        public override void Activate(Operator op)
        {
            caster = op;
            deployableBarUI = op.OperatorUI.DeployableBarUI;

            if (!op.IsDeployed || !op.CanUseSkill()) return;

            PlaySkillVFX(op);
            PlayAdditionalVFX(op);

            currentAmmo = maxAmmo;

            originalAttackSpeed = op.AttackSpeed;
            originalAttackDamage = op.AttackPower;

            // ���� ���� ���� 
            op.AttackPower *= attackDamageModifier;
            op.AttackSpeed /= attackSpeedModifier;

            // SPbar UI ������Ʈ
            deployableBarUI?.SwitchSPBarToAmmoMode(maxAmmo, currentAmmo);

            op.StartCoroutine(HandleSkillDuration(op)); // �ڷ�ƾ�� MonoBehaviour������ ��� ������
        }

        public override void OnAfterAttack(Operator op)
        {
            if (!op.IsSkillOn) return; 

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
            deployableBarUI?.UpdateAmmoDisplay(currentAmmo);
        }

        public virtual void TerminateSkill(Operator op)
        {
            OnSkillEnd(op);
            deployableBarUI?.SwitchSPBarToNormalMode();
        }

        protected override IEnumerator HandleSkillDuration(Operator op)
        {
            OnSkillStart(op);
            op.StartSkillDurationDisplay(duration);
            PlaySkillEffect(op);

            while (op.IsSkillOn)
            {
                // źȯ�� �������� ��ų ����
                if (currentAmmo <= 0)
                {
                    TerminateSkill(op);
                    yield break;
                }

                // �⺻) ���� ����
                yield return null;
            }

        }
    }
}
