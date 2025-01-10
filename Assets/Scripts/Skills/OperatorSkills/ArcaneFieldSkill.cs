
using UnityEngine;
using Skills.Base;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Arcane Field Skill", menuName = "Skills/Arcane Field Skill")]
    public class ArcaneFieldSkill : ActiveSkill
    {
        [Header("Damage Settings")]
        [SerializeField] private float damagePerTickRatio = 0.7f; // ����� ����
        [SerializeField] private float slowAmount = 0.3f; // �̵��ӵ� ������
        [SerializeField] private float fieldDuration = 3f; // ���� ���� �ð�
        [SerializeField] private float damageInterval = 0.5f; // ����� ����

        [Header("Visual Effect")]
        [SerializeField] private GameObject fieldEffectPrefab;

        // �߽� 8ĭ + 
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

            Enemy targetEnemy = op.CurrentTarget as Enemy; // ��ȯ ���н� null
            if (targetEnemy == null) return; // Ÿ���� ���� ���� ��ų ���

            op.CurrentSP = 0;

            // �� ��ġ�� �������� ���� ����
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
