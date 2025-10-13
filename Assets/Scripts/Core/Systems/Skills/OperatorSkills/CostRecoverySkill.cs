using UnityEngine;
using Skills.Base;

namespace SKills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Cost Recovery Skill", menuName = "Skills/Cost Recovery Skill")]
    public class CostRecoverySkill : PassiveSkill
    {
        [Header("Cost Recovery Settings")]
        [SerializeField] private int costRecoveryAmount = 8;
        [SerializeField] private GameObject costVFXPrefab = default!;

        protected override void SetDefaults()
        {
            autoRecover = true;
            autoActivate = true;
        }

        public override void Activate(Operator op)
        {
            if (op == null) return;

            RecoverCost(costRecoveryAmount); // 기능
            PlayEffect(op); // 이펙트

            op.CurrentSP = 0; // SP 초기화
        }

        private void PlayEffect(Operator op)
        {
            if (costVFXPrefab != null)
            {
                PlayVFX(op, GetCostVFXTag(op.OperatorData), op.transform.position, Quaternion.identity, 1);
            }
        }

        private void RecoverCost(float amount)
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.RecoverDeploymentCost(Mathf.RoundToInt(amount));
            }
        }


        public override void PreloadObjectPools(OperatorData ownerData)
        {
            base.PreloadObjectPools(ownerData);

            if (costVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(GetCostVFXTag(ownerData), costVFXPrefab, 1);
            }
        }

        public string GetCostVFXTag(OperatorData opData) => $"{opData.entityName}_{skillName}_costVFX";
        
    }


}