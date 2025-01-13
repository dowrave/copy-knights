
using UnityEngine;
using Skills.Base;
using System.Collections.Generic;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Arcane Field Skill", menuName = "Skills/Arcane Field Skill")]
    public class ArcaneFieldSkill : ActiveSkill
    {
        [Header("Damage Settings")]
        [SerializeField] private float damagePerTickRatio = 0.7f; // 대미지 배율
        [SerializeField] private float slowAmount = 0.3f; // 이동속도 감소율
        [SerializeField] private float damageInterval = 0.5f; // 대미지 간격

        [Header("References")]
        [SerializeField] private GameObject fieldEffectPrefab; // 실질적인 효과 프리팹
        [SerializeField] private GameObject skillRangeEffectPrefab; // 시각 효과 프리팹
        [SerializeField] private Color rangeEffectColor;
        private const string EFFECT_TAG = "ArcaneField";

        // 범위 
        private readonly List<Vector2Int> crossPattern = new List<Vector2Int>
        {
            new Vector2Int(-2, 0), new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, 2),
            new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(0, -2), new Vector2Int(1, 1),
            new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(2, 0)
        };

        protected override void SetDefaults()
        {
            autoRecover = true;
            autoActivate = false;
            modifiesAttackAction = true;
        }

        public override void Activate(Operator op)
        {
            Enemy targetEnemy = op.CurrentTarget as Enemy; // 변환 실패시 null
            if (targetEnemy == null) return; // 타겟이 없을 때는 스킬 취소

            base.Activate(op);

            // 스킬 범위 계산
            Vector2Int centerPos = MapManager.Instance.ConvertToGridPosition(targetEnemy.transform.position);
            HashSet<Vector2Int> affectedArea = CalculateAffectedArea(centerPos);

            // 유효한 타일에만 범위 이펙트 생성
            foreach (Vector2Int pos in affectedArea)
            {
                if (MapManager.Instance.CurrentMap.IsValidGridPosition(pos.x, pos.y))
                {
                    GameObject effectObj = ObjectPoolManager.Instance.SpawnFromPool(EFFECT_TAG,
                        MapManager.Instance.ConvertToWorldPosition(pos),
                        Quaternion.identity
                    );

                    var rangeEffect = effectObj.GetComponent<SkillRangeEffect>();
                    rangeEffect.Initialize(pos, affectedArea, rangeEffectColor, duration);
                }
            } 

            op.CurrentSP = 0;

            // 적 위치를 기준으로 장판 생성
            CreateDamageField(op, centerPos);
        }

        public override void PerformChangedAttackAction(Operator op)
        {
            // 스킬이 활성화된 동안 공격하지 않음 - 빈 칸
        }

        private HashSet<Vector2Int> CalculateAffectedArea(Vector2Int center)
        {
            HashSet<Vector2Int> affected = new HashSet<Vector2Int>();

            foreach (Vector2Int offset in crossPattern)
            {
                affected.Add(center + offset);
            }

            return affected;
        }

        private void CreateDamageField(Operator op, Vector2Int centerPos)
        {
            GameObject fieldObj = Instantiate(fieldEffectPrefab);
            ArcaneFieldController controller = fieldObj.GetComponent<ArcaneFieldController>(); 

            if (controller != null)
            {
                float actualDamagePerTick = op.AttackPower * damagePerTickRatio;
                controller.Initialize(op, centerPos, crossPattern, actualDamagePerTick, slowAmount, duration, damageInterval);
            }
        }

        public override void InitializeSkillObjectPool()
        {
            ObjectPoolManager.Instance.CreatePool(EFFECT_TAG, skillRangeEffectPrefab, crossPattern.Count);
        }

        public override void CleanupSkillObjectPool()
        {
            ObjectPoolManager.Instance.RemovePool(EFFECT_TAG);
        }
    }
}
