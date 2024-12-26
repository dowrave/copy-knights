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
            // ���� ȿ�� ����
            base.Activate(op);

            // ��ȣ�� Ȱ��ȭ
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
            // ��ȣ�� ����
            op.DeactivateShield();

            // VFX ����
            if (currentShieldEffect != null)
            {
                Destroy(currentShieldEffect);
                currentShieldEffect = null;
            }

            // ���� ���� ȿ�� ����
            base.OnSkillEnd(op);
        }
    }
}