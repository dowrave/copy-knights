using UnityEngine;
using System.Collections;

namespace Skills.Base
{
    // 탄환 수가 제한되어 있고 지속시간이 무한인 스킬의 추상 클래스
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

            // 스탯 변경 적용 
            op.AttackPower *= attackDamageModifier;
            op.AttackSpeed /= attackSpeedModifier;

            // 스킬 사용 직후에는 공격 속도 / 모션 초기화
            op.SetAttackDuration(0f);
            op.SetAttackCooldown(0f);

            // SPbar UI 업데이트
            operatorUI?.SwitchSPBarToAmmoMode(maxAmmo, currentAmmo);

            op.StartCoroutine(Co_HandleSkillDuration(op)); // 코루틴은 MonoBehaviour에서만 사용 가능함
        }

        public override void OnAfterAttack(Operator op)
        {
            // 스킬이 켜졌을 때에만 동작
            if (!op.IsSkillOn) return; 

            // 공격 후 탄환 소모
            ConsumeAmmo(op);

            // 탄환이 소진되면 스킬 종료
            if (CurrentAmmo <= 0)
            {
                TerminateSkill(op);
            }
            
        }

        protected virtual void ConsumeAmmo(Operator op)
        {
            currentAmmo = Mathf.Max(0, currentAmmo - 1);

            // 탄환 소모 후 UI 업데이트
            UpdateAmmoUI(op);
        }

        protected override void OnSkillEnd(Operator op)
        {
            // 스킬 종료 시 원래 스탯으로 복원
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
            // op.StartSkillDurationDisplay(duration?ㅉ);
            PlaySkillEffect(op);

            while (op.IsSkillOn)
            {
                // 오퍼레이터 파괴 시 동작을 멈춤
                if (op == null) yield break; 

                // 탄환이 떨어지면 스킬 종료
                if (currentAmmo <= 0)
                {
                    TerminateSkill(op); // 종료 로직은 이 안에 구현됨
                    yield break;
                }

                // 기본) 무한 지속
                yield return null;
            }
        }
    }
}
