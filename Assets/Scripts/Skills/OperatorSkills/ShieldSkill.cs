using UnityEngine;
using Skills.Base;
using UnityEngine.VFX;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Shield Skill", menuName = "Skills/Shield Skill")]
    public class ShieldSkill : BuffSkill
    {
        [Header("Shield Settings")]
        [SerializeField] private float shieldAmount = 500f;

        [SerializeField] private GameObject shieldEffectPrefab;
        private GameObject currentShieldEffect;
        private VisualEffect ShieldVFX;

        public override void Activate(Operator op)
        {
            // 버프 효과 적용 - 버프 이펙트 실행은 여기서 됨(방어력 상승 등)
            base.Activate(op);

            op.shieldSystem.OnShieldChanged += HandleShieldChanged;
            op.ActivateShield(shieldAmount);

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

        protected override void OnSkillEnd(Operator op)
        {
            op.shieldSystem.OnShieldChanged -= HandleShieldChanged;

            // 보호막 해제
            op.DeactivateShield();

            // VFX 제거
            if (currentShieldEffect != null)
            {
                Destroy(currentShieldEffect);
                currentShieldEffect = null;
                ShieldVFX = null;
            }

            // 기존 버프 효과 해제
            base.OnSkillEnd(op);
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