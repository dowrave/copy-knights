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

        private string _costVFXTag;

        protected override void SetDefaults()
        {
            autoRecover = true;
            autoActivate = true;
        }

        public override void OnSkillActivated(Operator op)
        {
            if (op == null) return;

            RecoverCost(costRecoveryAmount);
            PlayEffect(op); 

            op.SetCurrentSP(0f);
        }

        private void PlayEffect(Operator op)
        {
            if (costVFXPrefab != null)
            {
                PlayVFX(op, CostVFXTag, op.transform.position, Quaternion.identity, 1);
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
                ObjectPoolManager.Instance.CreatePool(CostVFXTag, costVFXPrefab, 1);
            }
        }

        public string CostVFXTag
        {
            get
            {
                if (string.IsNullOrEmpty(_costVFXTag))
                {
                    _costVFXTag = $"{skillName}_CostVFX";
                }
                return _costVFXTag;
            }
        }        

    }


}