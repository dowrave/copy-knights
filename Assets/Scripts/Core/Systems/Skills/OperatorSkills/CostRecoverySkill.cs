using UnityEngine;
using Skills.Base;

namespace SKills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Cost Recovery Skill", menuName = "Skills/Cost Recovery Skill")]
    public class CostRecoverySkill : PassiveSkill
    {
        [Header("Cost Recovery Settings")]
        [SerializeField] private int costRecoveryAmount = 8;
        [SerializeField] private GameObject skillEffectPrefab = default!;

        protected override void SetDefaults()
        {
            autoRecover = true;
            autoActivate = true;
        }

        public override void Activate(Operator op)
        {
            if (op == null) return;

            PlayEffect(op); // ����Ʈ

            RecoverCost(costRecoveryAmount); // ���

            op.CurrentSP = 0; // SP �ʱ�ȭ
        }

        private void PlayEffect(Operator op)
        {
            if (skillEffectPrefab != null)
            {
                GameObject effect = Instantiate(skillEffectPrefab, op.transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }

        private void RecoverCost(float amount)
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.RecoverDeploymentCost(Mathf.RoundToInt(amount));
            }
        }
    }
}