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

        protected Operator caster; // ��ų ������

        protected abstract void SetDefaults(); // �ν����� bool �ʵ尪�� �ʱ� ����.

        // ������ �ʿ��� �� ����
        public virtual void Activate(Operator op) { } // ��Ƽ�� ��ų�� ���� ���� ����
        public virtual void PerformChangedAttackAction(Operator op) { } // ���� �׼��� �����ؾ� �ϴ� ���
        public virtual void OnAttack(Operator op, ref float damage, ref bool showDamagePopup) { } // ���ݿ� ȿ���� �߰��ϴ� ���

        // ������Ʈ Ǯ���� ����� ���
        public virtual void InitializeSkillObjectPool() { } // ������Ʈ Ǯ ����
        public virtual void CleanupSkillObjectPool() { } // ������Ʈ Ǯ ����

        protected void Reset()
        {
            SetDefaults();
        }
    }

}