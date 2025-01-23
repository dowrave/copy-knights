using System.Collections;
using UnityEngine;

namespace Skills.Base
{
    public abstract class BaseSkill : ScriptableObject
    {
        [Header("Basic Skill Properties")]
        public string skillName;
        [TextArea(3, 10)]
        public string description;
        public float SPCost;
        public Sprite skillIcon;

        public bool autoRecover = false; // SP 자동회복 여부
        public bool autoActivate = false; // 자동발동 여부
        public bool modifiesAttackAction = false; // 기본 공격이 다르게 나가는 스킬일 때 true

        protected Operator caster; // 스킬 시전자

        protected abstract void SetDefaults(); // 인스펙터 bool 필드값들 초기 설정.

        // 동작이 필요할 때 구현
        public virtual void Activate(Operator op) { } // 액티브 스킬을 켰을 때의 동작
        public virtual void PerformChangedAttackAction(Operator op) { } // 공격 액션을 수정해야 하는 경우
        public virtual void OnAttack(Operator op, ref float damage, ref bool showDamagePopup) { } // 공격에 효과를 추가하는 경우

        // 오브젝트 풀링을 사용할 경우
        public virtual void InitializeSkillObjectPool() { } // 오브젝트 풀 구현
        public virtual void CleanupSkillObjectPool() { } // 오브젝트 풀 제거

        protected void Reset()
        {
            SetDefaults();
        }
    }

}