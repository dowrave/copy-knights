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

            // 지속 시간이 없는 경우는 없다고 하겠음 일단
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
            // 방어력과 마법 저항력 증가
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

                // VFX 컴포넌트 캐싱
                ShieldVFX = currentShieldEffect.GetComponent<VisualEffect>();
                if (ShieldVFX != null)
                {
                    ShieldVFX.Play();
                }
            }
        }

        private void HandleShieldChanged(float currentShield, bool isShieldDepleted)
        {
            // 보호막 완전히 소진되었을 때
            if (isShieldDepleted && ShieldVFX != null)
            {
                ShieldVFX.Stop();

                if (currentShieldEffect != null)
                {
                    Destroy(currentShieldEffect, 0.5f); // 페이드아웃
                }
            }
        }
    }


}