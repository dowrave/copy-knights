using UnityEngine;
using Skills.Base;
using UnityEngine.VFX;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Shield Skill", menuName = "Skills/Shield Skill")]
    public class ShieldSkill : ActiveSkill
    {
        [Header("Shield Settings")]
        [SerializeField] private float shieldAmount = 500f;

        [Header("Stat Boost Settings")]
        [SerializeField] private StatModifiers statModifiers = new StatModifiers();

        [Header("Shield Visual Effects")]
        [SerializeField] private GameObject shieldEffectPrefab;
        private GameObject currentShieldEffect;
        private VisualEffect ShieldVFX;

        private float originalDefense;
        private float originalMagicResistance;

        [System.Serializable]
        public class StatModifiers
        {
            public float defenseModifier = 1.2f;
            public float magicResistanceModifier = 1f;
        }

        protected override void SetDefaults()
        {
            autoRecover = true;
            modifiesAttackAction = true;
        }

        public override void Activate(Operator op)
        {
            if (!op.IsDeployed || !op.CanUseSkill()) return;

            StoreOriginalStats(op);

            ApplyStatModifiers(op);

            op.shieldSystem.OnShieldChanged += HandleShieldChanged;

            op.ActivateShield(shieldAmount);

            base.PlaySkillEffect(op);
            PlayAdditionalEffects(op);

            // ���� �ð��� ���� ���� ���ٰ� �ϰ��� �ϴ�
            op.StartCoroutine(HandleSkillDuration(op));
        }

        protected override void OnSkillEnd(Operator op)
        {
            op.DeactivateShield();

            op.shieldSystem.OnShieldChanged -= HandleShieldChanged;

            RestoreOriginalStats(op);

            if (currentShieldEffect != null)
            {
                SafeDestroySkillEffect(currentShieldEffect);
                ShieldVFX = null;
                currentShieldEffect = null;
            }

            base.OnSkillEnd(op);
        }

        private void StoreOriginalStats(Operator op)
        {
            originalDefense = op.Defense;
            originalMagicResistance = op.MagicResistance;
        }

        private void ApplyStatModifiers(Operator op)
        {
            // ���°� ���� ���׷� ����
            op.Defense *= statModifiers.defenseModifier;
            op.MagicResistance *= statModifiers.magicResistanceModifier;
        }

        private void RestoreOriginalStats(Operator op)
        {
            op.Defense = originalDefense;
            op.MagicResistance = originalMagicResistance;
        }

        protected override void PlayAdditionalEffects(Operator op)
        {
            if (shieldEffectPrefab != null)
            {
                Vector3 effectPosition = op.transform.position;
                currentShieldEffect = Instantiate(shieldEffectPrefab, effectPosition, Quaternion.identity);
                currentShieldEffect.transform.SetParent(op.transform);

                // VFX ������Ʈ ĳ��
                ShieldVFX = currentShieldEffect.GetComponent<VisualEffect>();
                if (ShieldVFX != null)
                {
                    ShieldVFX.Play();
                }
            }
        }

        private void HandleShieldChanged(float currentShield, bool isShieldDepleted)
        {
            // ��ȣ�� ������ �����Ǿ��� ��
            if (isShieldDepleted && ShieldVFX != null)
            {
                ShieldVFX.Stop();

                if (currentShieldEffect != null)
                {
                    Destroy(currentShieldEffect, 0.5f); // ���̵�ƿ�
                }
            }
        }
    }


}