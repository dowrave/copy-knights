using System.Collections;
using UnityEngine;

namespace Skills.Base
{
    public abstract class OperatorSkill : UnitSkill
    {
        [Header("Operator Base Skill Properties")]
        [TextArea(3, 10)]
        public string description = string.Empty;
        public float spCost = 0f;
        public Sprite skillIcon = default!;

        public bool autoRecover = false; // 활성화시 자동 회복, 비활성화 시 공격 시 회복
        public bool autoActivate = false; // 자동발동 여부

        [Header("테스트 여부 설정 : 테스트 시에 SP = 1")]
        public bool isOnTest = false; // 현재 테스트 여부, 테스트 시 SP = 1로 설정

        public float SPCost => isOnTest ? 1f : spCost; // 테스트 모드일 때 SP 비용을 1로 설정
        
        // 이펙트를 변경해야 하는 경우
        [Header("Attack VFX Overrides(Optional)")]
        public GameObject meleeAttackVFXOverride;

        // 인스펙터 bool 필드값들 초기 설정.
        protected virtual void SetDefaults() { }

        // 액티브 스킬을 켰을 때의 동작
        public virtual void Activate(Operator op) { }

        // modifiesAttackAction가 true일 때, 공격을 변경하는 액션
        public virtual void PerformChangedAttackAction(Operator op) { }

        // op.Attack()을 사용하고, 공격이 적용되기 전 효과 반영
        public virtual void OnBeforeAttack(Operator op, ref float damage, ref AttackType finalAttackType, ref bool showDamagePopup) { }

        // 스킬이 프레임 상태를 확인하도록 하는 훅
        public virtual void OnUpdate(Operator op) { }


        public virtual void OnAfterAttack(Operator op, UnitEntity target) { }

        /// <summary>
        /// 스킬에서 사용되는 오브젝트 풀 생성(VFX 포함)
        /// </summary>
        public virtual void PreloadObjectPools(OperatorData ownerData)
        {
            // 근접 공격 VFX 변경
            if (meleeAttackVFXOverride != null)
            {
                ObjectPoolManager.Instance.CreatePool(GetMeleeAttackVFXTag(ownerData), meleeAttackVFXOverride, 2);
            }
        }

        public string GetMeleeAttackVFXTag(OperatorData ownerData) => $"{ownerData.entityName}_{skillName}_MeleeVFX";

        protected void Reset()
        {
            SetDefaults();
        }

        #region

        // UnitSkill로 타입이 오더라도 안전하게 실행시키기 위한 메서드들
        // sealed는 하위 클래스의 오버라이드를 방지함 
        public sealed override void Activate(UnitEntity caster)
        {
            if (caster is Operator op)
            {
                Activate(op);
            }
        }
        
            // -- 타입 안정성을 위한 구현
        public sealed override void OnBeforeAttack(UnitEntity caster, UnitEntity target, ref float damage, ref AttackType attackType)
        {
            // 일단 false로 둠
            bool showDamagePopup = false;

            if (caster is Operator op)
            {
                OnBeforeAttack(op, ref damage, ref attackType, ref showDamagePopup);
            }
        }
        
            // op.Attack()을 사용하고, 공격이 적용된 후 효과 반영
        public sealed override void OnAfterAttack(UnitEntity caster, UnitEntity target)
        {
            if (caster is Operator op)
            {
                OnAfterAttack(op, target);
            }
        }

        #endregion
    }

    }