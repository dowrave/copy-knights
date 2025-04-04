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

            // ����Ʈ ����
            GameObject effect = Instantiate(skillEffectPrefab, op.transform.position, Quaternion.identity);

            // 2�� �� ����Ʈ �ڵ� ����
            Destroy(effect, 2f); 

            RecoverDeploymentCost(costRecoveryAmount);
            op.CurrentSP = 0;
        }

        private void RecoverDeploymentCost(float amount)
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.RecoverDeploymentCost(Mathf.RoundToInt(amount));
            }
        }
    }
}