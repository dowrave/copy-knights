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
        public float SPCost = 0f;
        public Sprite skillIcon = default!;

        public bool autoRecover = false; // SP �ڵ�ȸ�� ����
        public bool autoActivate = false; // �ڵ��ߵ� ����
        public bool modifiesAttackAction = false; // �⺻ ������ �ٸ��� ������ ��ų�� �� true

        protected Operator caster = default!; // ��ų ������

        protected abstract void SetDefaults(); // �ν����� bool �ʵ尪�� �ʱ� ����.

        // ������ �ʿ��� �� ����
        public virtual void Activate(Operator op) { } // ��Ƽ�� ��ų�� ���� ���� ����
        public virtual void PerformChangedAttackAction(Operator op) { } // ���� �׼��� �����ؾ� �ϴ� ���
        public virtual void OnAttack(Operator op, ref float damage, ref bool showDamagePopup) { } // ���ݿ� ȿ���� �߰��ϴ� ���

        // ������Ʈ Ǯ���� ����� ���
        public virtual void InitializeSkillObjectPool() { } // ������Ʈ Ǯ ����
        public virtual void CleanupSkill() { } // ��ų�� ���õ� ���ҽ� ����

        protected void Reset()
        {
            SetDefaults();
        }
    }

}