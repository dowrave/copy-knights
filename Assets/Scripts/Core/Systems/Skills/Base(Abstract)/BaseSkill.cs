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

        [Header("�׽�Ʈ ���� ���� : �׽�Ʈ �ÿ� SP = 1")]
        public bool isOnTest = false; // ���� �׽�Ʈ ����, �׽�Ʈ �� SP = 1�� ����

        public float SPCost => isOnTest ? 1f : spCost; // �׽�Ʈ ����� �� SP ����� 1�� ����

        protected Operator caster = default!; // ��ų ������

        // ����Ʈ�� �����ؾ� �ϴ� ���
        [Header("Attack VFX Overrides(Optional)")]
        public GameObject meleeAttackEffectOverride;

        protected string MELEE_ATTACK_OVERRIDE_TAG;

        // �ν����� bool �ʵ尪�� �ʱ� ����.
        protected virtual void SetDefaults() { }

        // ��Ƽ�� ��ų�� ���� ���� ����
        public virtual void Activate(Operator op) { } 

         // modifiesAttackAction�� true�� ��, ������ �����ϴ� �׼�
        public virtual void PerformChangedAttackAction(Operator op) { }

        // op.Attack()�� ����ϰ�, ������ ����Ǳ� �� ȿ�� �ݿ�
        public virtual void OnBeforeAttack(Operator op, ref float damage, ref AttackType finalAttackType, ref bool showDamagePopup){ }

        // ��ų�� ������ ���¸� Ȯ���ϵ��� �ϴ� ��
        public virtual void OnUpdate(Operator op) { }
        
        // op.Attack()�� ����ϰ�, ������ ����� �� ȿ�� �ݿ�
        public virtual void OnAfterAttack(Operator op) { }

        /// <summary>
        /// ��ų���� ���Ǵ� ������Ʈ Ǯ ����(VFX ����)
        /// </summary>
        public virtual void InitializeSkillObjectPool(UnitEntity caster)
        {
            // ���� ���� VFX ����
            if (meleeAttackEffectOverride != null)
            {
                MELEE_ATTACK_OVERRIDE_TAG = RegisterPool(caster, meleeAttackEffectOverride);
            }
        }

        /// <summary>
        /// ��ų�� ���õ� ������Ʈ Ǯ�� ����Ѵ�. �±׸� ��ȯ�Ѵ�.
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
            else return string.Empty; // �ϴ� �����
        }

        protected void Reset()
        {
            SetDefaults();
        }
    }

}