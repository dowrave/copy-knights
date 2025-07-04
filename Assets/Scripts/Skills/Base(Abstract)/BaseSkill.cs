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
        public bool modifiesAttackAction = false; // 공격 액션 변경 여부

        [Header("테스트 여부 설정 : 테스트 시에 SP = 1")]
        public bool isOnTest = false; // 현재 테스트 여부, 테스트 시 SP = 1로 설정

        public float SPCost => isOnTest ? 1f : spCost; // 테스트 모드일 때 SP 비용을 1로 설정

        protected Operator caster = default!; // 스킬 시전자


        // 인스펙터 bool 필드값들 초기 설정.
        protected abstract void SetDefaults(); 

        // 액티브 스킬을 켰을 때의 동작
        public virtual void Activate(Operator op) { } 

         // modifiesAttackAction가 true일 때, 공격을 변경하는 액션
        public virtual void PerformChangedAttackAction(Operator op) { }

        // op.Attack()을 사용하고, 공격이 적용되기 전 효과 반영
        public virtual void OnBeforeAttack(Operator op, ref float damage, ref bool showDamagePopup){ } 
        
        // op.Attack()을 사용하고, 공격이 적용된 후 효과 반영
        public virtual void OnAfterAttack(Operator op) { } 

        // 오브젝트 풀링을 사용할 경우
        public virtual void InitializeSkillObjectPool() { } // 오브젝트 풀 구현
        public virtual void CleanupSkill() { } // 스킬에 관련된 리소스 제거

        protected void Reset()
        {
            SetDefaults();
        }
    }

}