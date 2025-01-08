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

        // 액티브 스킬을 켰을 때의 동작
        public virtual void Activate(Operator op) { }
        
        // 공격 액션을 수정해야 하는 경우 실행
        public virtual void PerformSkillAction(Operator op) { }
        public virtual void OnAttack(Operator op, ref float damage, ref bool showDamagePopup) { }

        // 인스펙터 bool 필드값들 초기 설정. 의도에 따라 얼마든지 달라질 수 있음
        protected abstract void SetDefaults();

        protected void Reset()
        {
            SetDefaults();
        }
    }

}