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
            // ���� ȿ�� ���� - ���� ����Ʈ ������ ���⼭ ��(���� ��� ��)
            base.Activate(op);

            op.shieldSystem.OnShieldChanged += HandleShieldChanged;
            op.ActivateShield(shieldAmount);

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

        protected override void OnSkillEnd(Operator op)
        {
            op.shieldSystem.OnShieldChanged -= HandleShieldChanged;

            // ��ȣ�� ����
            op.DeactivateShield();

            // VFX ����
            if (currentShieldEffect != null)
            {
                Destroy(currentShieldEffect);
                currentShieldEffect = null;
                ShieldVFX = null;
            }

            // ���� ���� ȿ�� ����
            base.OnSkillEnd(op);
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