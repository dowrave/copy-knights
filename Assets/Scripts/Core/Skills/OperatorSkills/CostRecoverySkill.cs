using UnityEngine;
using Skills.Base;

namespace SKills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Cost Recovery Skill", menuName = "Skills/Cost Recovery Skill")]
    public class CostRecoverySkill : Skill
    {
        [SerializeField] private GameObject skillEffectPrefab; 

        [Header("Cost Recovery Settings")]
        [SerializeField] private int costRecoveryAmount = 8;
        [SerializeField] private bool isAutoActivate = true;
        [SerializeField] private float skillDuration = 0f;

        public override bool AutoRecover => true;
        public override bool AutoActivate => isAutoActivate;

        private OperatorStats originalStats;
        private bool bonusesApplied = false;

        public override void Activate(Operator op)
        {
            if (op == null) return;

            // 이펙트 생성
            GameObject effect = Instantiate(skillEffectPrefab, op.transform.position + Vector3.up, Quaternion.identity);

            // 2초 후 이펙트 자동 제거
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