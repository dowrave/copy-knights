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

        public bool autoRecover = false; // Ȱ��ȭ�� �ڵ� ȸ��, ��Ȱ��ȭ �� ���� �� ȸ��
        public bool autoActivate = false; // �ڵ��ߵ� ����

        [Header("�׽�Ʈ ���� ���� : �׽�Ʈ �ÿ� SP = 1")]
        public bool isOnTest = false; // ���� �׽�Ʈ ����, �׽�Ʈ �� SP = 1�� ����

        public float SPCost => isOnTest ? 1f : spCost; // �׽�Ʈ ����� �� SP ����� 1�� ����

        // protected Operator caster = default!; // ��ų ������

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
        public virtual void OnBeforeAttack(Operator op, ref float damage, ref AttackType finalAttackType, ref bool showDamagePopup) { }

        // ��ų�� ������ ���¸� Ȯ���ϵ��� �ϴ� ��
        public virtual void OnUpdate(Operator op) { }


        public virtual void OnAfterAttack(Operator op, UnitEntity target) { }

        /// <summary>
        /// ��ų���� ���Ǵ� ������Ʈ Ǯ ����(VFX ����)
        /// </summary>
        public virtual void PreloadObjectPools(OperatorData ownerData)
        {
            // ���� ���� VFX ����
            if (meleeAttackEffectOverride != null)
            {
                MELEE_ATTACK_OVERRIDE_TAG = RegisterPool(ownerData, meleeAttackEffectOverride);
            }
        }

        protected string RegisterPool(OperatorData ownerData, GameObject prefab, int initialSize = 5)
        {
            if (prefab == null) return string.Empty;

            string poolTag = GetVFXPoolTag(ownerData, prefab);
            if (!ObjectPoolManager.Instance.IsPoolExist(poolTag))
            {
                ObjectPoolManager.Instance.CreatePool(poolTag, prefab, initialSize);
            }
            return poolTag; 
        }

        public virtual string GetVFXPoolTag(OperatorData ownerData, GameObject vfxPrefab)
        {
            if (vfxPrefab == null)
            {
                Debug.LogError("[BaseSkill.GetVFXPoolTag] vfxPrefab�� null��!!");
                return string.Empty;
            }

            return $"{ownerData.entityName}_{this.name}_{vfxPrefab.name}";

            // if (caster is Operator op)
            // {
            //     return $"{op.OperatorData.entityName}_{this.name}_{vfxPrefab.name}";
            // }

            // return string.Empty;
        }

        protected void Reset()
        {
            SetDefaults();
        }

        #region

        // UnitSkill�� Ÿ���� ������ �����ϰ� �����Ű�� ���� �޼����
        // sealed�� ���� Ŭ������ �������̵带 ������ 
        public sealed override void Activate(UnitEntity caster)
        {
            if (caster is Operator op)
            {
                Activate(op);
            }
        }
        
            // -- Ÿ�� �������� ���� ����
        public sealed override void OnBeforeAttack(UnitEntity caster, UnitEntity target, ref float damage, ref AttackType attackType)
        {
            // �ϴ� false�� ��
            bool showDamagePopup = false;

            if (caster is Operator op)
            {
                OnBeforeAttack(op, ref damage, ref attackType, ref showDamagePopup);
            }
        }
        
            // op.Attack()�� ����ϰ�, ������ ����� �� ȿ�� �ݿ�
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