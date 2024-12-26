using UnityEngine;
using Skills.Base;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Shield Skill", menuName = "Skills/Shield Skill")]
    public class ShieldSkill : BuffSkill
    {
        [Header("Shield Settings")]
        [SerializeField] private float shieldAmount = 500f;

        [SerializeField] private GameObject shieldEffectPrefab;
        private GameObject currentShieldEffect;


        public override void Activate(Operator op)
        {
            // 버프 효과 적용
            base.Activate(op);

            // 보호막 활성화
            op.ActivateShield(shieldAmount);

            if (shieldEffectPrefab != null)
            {
                Vector3 effectPosition = op.transform.position;
                currentShieldEffect = Instantiate(shieldEffectPrefab, effectPosition, Quaternion.identity);
                currentShieldEffect.transform.SetParent(op.transform);
            }
        }

        protected override void OnSkillEnd(Operator op)
        {
            // 보호막 해제
            op.DeactivateShield();

            // VFX 제거
            if (currentShieldEffect != null)
            {
                Destroy(currentShieldEffect);
                currentShieldEffect = null;
            }

            // 기존 버프 효과 해제
            base.OnSkillEnd(op);
        }
    }
}