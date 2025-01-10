
using UnityEngine;
using Skills.Base;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Arcane Field Skill", menuName = "Skills/Arcane Field Skill")]
    public class ArcaneFieldSkill : ActiveSkill
    {
        [Header("Damage Settings")]
        [SerializeField] private float damagePerTickRatio = 0.7f; // 대미지 배율
        [SerializeField] private float slowAmount = 0.3f; // 이동속도 감소율
        [SerializeField] private float fieldDuration = 3f; // 장판 지속 시간
        [SerializeField] private float damageInterval = 0.5f; // 대미지 간격

        [Header("Visual Effect")]
        [SerializeField] private GameObject fieldEffectPrefab;

        // 중심 8칸 + 
        private readonly Vector2Int[] crossPattern = new Vector2Int[]
        {
            new Vector2Int(-2, 0), new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, 2),
            new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(0, -2), new Vector2Int(1, 1),
            new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(2, 0)
        };

        protected override void SetDefaults()
        {
            autoRecover = true;
            autoActivate = false;
        }

        public override void Activate(Operator op)
        {
            if (!op.IsDeployed || !op.CanUseSkill()) return;

            Enemy targetEnemy = op.CurrentTarget as Enemy; // 변환 실패시 null
            if (targetEnemy == null) return; // 타겟이 없을 때는 스킬 취소

            op.CurrentSP = 0;

            // 적 위치를 기준으로 장판 생성
            Vector2Int centerPos = MapManager.Instance.ConvertToGridPosition(targetEnemy.transform.position);
            CreateDamageField(op, centerPos);
        }

        private void CreateDamageField(Operator op, Vector2Int centerPos)
        {
            GameObject fieldObj = Instantiate(fieldEffectPrefab);
            ArcaneFieldController controller = fieldObj.GetComponent<ArcaneFieldController>(); 

            if (controller != null)
            {
                float actualDamagePerTick = op.AttackPower * damagePerTickRatio;
                controller.Initialize(op, centerPos, crossPattern, actualDamagePerTick, slowAmount, fieldDuration, damageInterval);
            }
        }
    }

}
