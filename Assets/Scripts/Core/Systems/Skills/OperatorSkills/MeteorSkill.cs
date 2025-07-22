using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Skills.Base
{
    [CreateAssetMenu(fileName = "New Meteor Skill", menuName = "Skills/Meteor Skill")]
    public class MateorSkill : ActiveSkill
    {
        [Header("MateorSkill Settings")]
        [SerializeField] private float damageMultiplier = 0.5f;
        [SerializeField] private float stunDuration = 2f;
        [SerializeField] private int costRecovery = 10;
        [SerializeField] private GameObject meteorPrefab = default!; // 떨어지는 메쉬 자체
        [SerializeField] private Vector2 meteorHeights = new Vector2(4f, 5f); // 두 오브젝트의 높이
        [SerializeField] private float meteorDelay = 0.5f;
        [SerializeField] private GameObject hitEffectPrefab = default!;
        [SerializeField] private GameObject skillRangeVFXPrefab = default!;
        private string hitEffectTag = "MeteorHit";

        protected override void SetDefaults()
        {
            duration = 0f; // 즉발성 스킬 명시
        }

        protected override void PlaySkillEffect(Operator op)
        {
            if (hitEffectPrefab == null)
            {
                hitEffectPrefab = op.OperatorData.HitEffectPrefab;
            }

            // 코스트 회복
            StageManager.Instance!.RecoverDeploymentCost(costRecovery);

            // 범위 계산
            caster = op;
            Vector2Int centerPos = MapManager.Instance!.ConvertToGridPosition(op.transform.position);
            actualSkillRange.Clear();
            CalculateActualSkillRange(centerPos);
            VisualizeSkillRange(op, actualSkillRange);

            // 중복 타격 방지를 위한 ID 세트
            var enemyIdSet = new HashSet<int>();

            // 범위 내의 모든 적에게 메테오 소환
            foreach (Vector2Int pos in actualSkillRange)
            {
                Tile? tile = MapManager.Instance!.GetTile(pos.x, pos.y);
                if (tile != null)
                {
                    foreach (Enemy enemy in tile.EnemiesOnTile.ToList())
                    {
                        if (enemyIdSet.Add(enemy.GetInstanceID()))
                        {
                            CreateMeteorSequence(op, enemy);
                        }
                    }
                }
            }
        }

        private void CreateMeteorSequence(Operator op, Enemy target)
        {
            CreateMeteor(op, target, meteorHeights.x, 0f);
            CreateMeteor(op, target, meteorHeights.y, meteorDelay);
        }

        private void CreateMeteor(Operator op, Enemy target, float height, float delayTime)
        {
            Vector3 spawnPos = target.transform.position + Vector3.up * height;
            GameObject meteorObj = Instantiate(meteorPrefab, spawnPos, Quaternion.Euler(90, 0, 0), target.transform);

            MeteorController? controller = meteorObj.GetComponent<MeteorController>();

            if (controller != null)
            {
                float actualDamage = op.AttackPower * damageMultiplier;
                controller.Initialize(op, target, actualDamage, delayTime, stunDuration, hitEffectPrefab, hitEffectTag);
            }
        }

        public override void InitializeSkillObjectPool(UnitEntity caster)
        {
            base.InitializeSkillObjectPool(caster);
            if (caster is Operator op)
            {
                if (meteorPrefab != null) ObjectPoolManager.Instance!.CreatePool($"{op.OperatorData.entityName}_{this.name}_Meteor", meteorPrefab, 10);
                if (hitEffectPrefab != null) ObjectPoolManager.Instance!.CreatePool($"{op.OperatorData.entityName}_{this.name}_HitEffect", hitEffectPrefab, 10);
                if (skillRangeVFXPrefab != null) ObjectPoolManager.Instance!.CreatePool($"{op.OperatorData.entityName}_{this.name}_RangeVFX", skillRangeVFXPrefab, skillRangeOffset.Count);
            }
        }

        private void VisualizeSkillRange(Operator op, HashSet<Vector2Int> range)
        {
            string vfxPoolTag = $"{this.name}_RangeVFX";

            foreach (Vector2Int pos in range)
            {
                if (MapManager.Instance!.CurrentMap!.IsValidGridPosition(pos.x, pos.y))
                {
                    Vector3 worldPos = MapManager.Instance!.ConvertToWorldPosition(pos);
                    GameObject? vfxObj = ObjectPoolManager.Instance?.SpawnFromPool(vfxPoolTag, worldPos, Quaternion.identity);

                    if (vfxObj != null)
                    {
                        // 즉발성 스킬의 시각 효과는 짧게 유지되므로,
                        // 부모를 설정하지 않아도 스스로 사라지게 하는 것이 더 나을 수 있습니다.
                        // 또는 op가 죽었을 때 함께 사라지게 하려면 부모 설정이 좋습니다. (선택의 문제)
                        vfxObj.transform.SetParent(op.transform);

                        var controller = vfxObj.GetComponent<SkillRangeVFXController>();
                        if (controller != null)
                        {
                            // duration이 0이므로, 컨트롤러는 내부적으로 짧은 시간(예: 1초) 동안만 표시합니다.
                            controller.Initialize(pos, range, this.duration);
                        }
                    }
                }
            }
        }
    }
}

