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
                PlayVFX(op, GetCostVFXTag(op), op.transform.position, Quaternion.identity, 1);
            }
        }

        private void RecoverCost(float amount)
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.RecoverDeploymentCost(Mathf.RoundToInt(amount));
            }
        }


        public override void InitializeSkillObjectPool(UnitEntity caster)
        {
            if (costVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(GetCostVFXTag(caster), costVFXPrefab, 1);
            }
        }

        public string GetCostVFXTag(UnitEntity caster) => $"{caster.name}_{skillName}_costVFX";
        
    }


}