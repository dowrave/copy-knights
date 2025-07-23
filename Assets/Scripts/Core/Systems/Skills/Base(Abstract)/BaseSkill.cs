using System.Collections;
using UnityEngine;

namespace Skills.Base
{
    public abstract class BaseSkill : ScriptableObject
    {
        [Header("Basic Skill Properties")]
        public string skillName = string.Empty;
        [TextArea(3, 10)]
        public string description = string.Empty;
        public float spCost = 0f;
        public Sprite skillIcon = default!;

        public bool autoRecover = false; // 활성화시 자동 회복, 비활성화 시 공격 시 회복
        public bool autoActivate = false; // 자동발동 여부

        [Header("테스트 여부 설정 : 테스트 시에 SP = 1")]
        public bool isOnTest = false; // 현재 테스트 여부, 테스트 시 SP = 1로 설정

        public float SPCost => isOnTest ? 1f : spCost; // 테스트 모드일 때 SP 비용을 1로 설정

        protected Operator caster = default!; // 스킬 시전자

        // 이펙트를 변경해야 하는 경우
        [Header("Attack VFX Overrides(Optional)")]
        public GameObject meleeAttackEffectOverride;

        protected string MELEE_ATTACK_OVERRIDE_TAG;

        // 인스펙터 bool 필드값들 초기 설정.
        protected virtual void SetDefaults() { }

        // 액티브 스킬을 켰을 때의 동작
        public virtual void Activate(Operator op) { } 

         // modifiesAttackAction가 true일 때, 공격을 변경하는 액션
        public virtual void PerformChangedAttackAction(Operator op) { }

        // op.Attack()을 사용하고, 공격이 적용되기 전 효과 반영
        public virtual void OnBeforeAttack(Operator op, ref float damage, ref AttackType finalAttackType, ref bool showDamagePopup){ }

        // 스킬이 프레임 상태를 확인하도록 하는 훅
        public virtual void OnUpdate(Operator op) { }
        
        // op.Attack()을 사용하고, 공격이 적용된 후 효과 반영
        public virtual void OnAfterAttack(Operator op) { }

        /// <summary>
        /// 스킬에서 사용되는 오브젝트 풀 생성(VFX 포함)
        /// </summary>
        public virtual void InitializeSkillObjectPool(UnitEntity caster)
        {
            // 근접 공격 VFX 변경
            if (meleeAttackEffectOverride != null)
            {
                MELEE_ATTACK_OVERRIDE_TAG = RegisterPool(caster, meleeAttackEffectOverride);
            }
        }

        /// <summary>
        /// 스킬과 관련된 오브젝트 풀을 등록한다. 태그를 반환한다.
        /// </summary>
        protected virtual string RegisterPool(UnitEntity caster, GameObject prefab, int initialSize = 5)
        {
            if (prefab == null) return null;

            string poolTag = GetVFXPoolTag(caster, prefab);
            ObjectPoolManager.Instance.CreatePool(poolTag, prefab, initialSize);
            return poolTag;
        }

        public string GetVFXPoolTag(UnitEntity caster, GameObject vfxPrefab)
        {
            if (vfxPrefab == null) return string.Empty;
            if (caster is Operator op)
            {
                return $"{op.OperatorData.entityName}_{this.name}_{vfxPrefab.name}";
            }
            else return string.Empty; // 일단 비워둠
        }

        protected void Reset()
        {
            SetDefaults();
        }
    }

}