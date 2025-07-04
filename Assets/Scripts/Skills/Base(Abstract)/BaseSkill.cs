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

        public bool autoRecover = false; // Ȱ��ȭ�� �ڵ� ȸ��, ��Ȱ��ȭ �� ���� �� ȸ��
        public bool autoActivate = false; // �ڵ��ߵ� ����
        public bool modifiesAttackAction = false; // ���� �׼� ���� ����

        [Header("�׽�Ʈ ���� ���� : �׽�Ʈ �ÿ� SP = 1")]
        public bool isOnTest = false; // ���� �׽�Ʈ ����, �׽�Ʈ �� SP = 1�� ����

        public float SPCost => isOnTest ? 1f : spCost; // �׽�Ʈ ����� �� SP ����� 1�� ����

        protected Operator caster = default!; // ��ų ������


        // �ν����� bool �ʵ尪�� �ʱ� ����.
        protected abstract void SetDefaults(); 

        // ��Ƽ�� ��ų�� ���� ���� ����
        public virtual void Activate(Operator op) { } 

         // modifiesAttackAction�� true�� ��, ������ �����ϴ� �׼�
        public virtual void PerformChangedAttackAction(Operator op) { }

        // op.Attack()�� ����ϰ�, ������ ����Ǳ� �� ȿ�� �ݿ�
        public virtual void OnBeforeAttack(Operator op, ref float damage, ref bool showDamagePopup){ } 
        
        // op.Attack()�� ����ϰ�, ������ ����� �� ȿ�� �ݿ�
        public virtual void OnAfterAttack(Operator op) { } 

        // ������Ʈ Ǯ���� ����� ���
        public virtual void InitializeSkillObjectPool() { } // ������Ʈ Ǯ ����
        public virtual void CleanupSkill() { } // ��ų�� ���õ� ���ҽ� ����

        protected void Reset()
        {
            SetDefaults();
        }
    }

}