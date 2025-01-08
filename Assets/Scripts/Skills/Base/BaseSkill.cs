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

        public bool autoRecover = false; // SP �ڵ�ȸ�� ����
        public bool autoActivate = false; // �ڵ��ߵ� ����
        public bool modifiesAttackAction = false; // �⺻ ������ �ٸ��� ������ ��ų�� �� true

        // ��Ƽ�� ��ų�� ���� ���� ����
        public virtual void Activate(Operator op) { }
        
        // ���� �׼��� �����ؾ� �ϴ� ��� ����
        public virtual void PerformSkillAction(Operator op) { }
        public virtual void OnAttack(Operator op, ref float damage, ref bool showDamagePopup) { }

        // �ν����� bool �ʵ尪�� �ʱ� ����. �ǵ��� ���� �󸶵��� �޶��� �� ����
        protected abstract void SetDefaults();

        protected void Reset()
        {
            SetDefaults();
        }
    }

}