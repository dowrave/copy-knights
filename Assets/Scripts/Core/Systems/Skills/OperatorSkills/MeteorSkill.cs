using UnityEngine;
using System.Collections;
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
        [SerializeField] private Vector2 meteorHeights = new Vector2(); // 두 오브젝트의 높이
        [SerializeField] private GameObject hitVFXPrefab = default!;
        [SerializeField] private GameObject skillRangeVFXPrefab = default!;
        [SerializeField] private float fallSpeed = 10f;

        private float meteorDelay = 0.5f;

        private string _meteorTag;
        private string _skillRangeVFXTag;
        private string _hitVFXTag;

        protected override void SetDefaults()
        {
            duration = 0f; // 즉발성 스킬 명시
        }

        public override void OnSkillActivated(Operator caster)
        {
            if (hitVFXPrefab == null)
            {
                hitVFXPrefab = caster.OperatorData.HitEffectPrefab;
            }

            // 코스트 회복
            StageManager.Instance!.RecoverDeploymentCost(costRecovery);

            // 범위 계산
            Vector2Int centerPos = GetCenterGridPos(caster);

            // 이전 스킬과 중심 위치가 달라진 경우에는 새로 범위를 계산함
            if (centerPos != caster.LastSkillCenter)
            {
                HashSet<Vector2Int> skillRange = PositionCalculationSystem.CalculateRange(skillRangeOffset, centerPos, caster.FacingDirection.Value);
                caster.SetCurrentSkillRange(skillRange);
                caster.SetLastSkillCenter(centerPos);
            }

            VisualizeSkillRange(caster, caster.GetCurrentSkillRange());

            // 중복 타격 방지를 위한 ID 세트
            var enemyIdSet = new HashSet<int>();

            // 범위 내의 모든 적에게 메테오 소환
            foreach (Vector2Int pos in caster.GetCurrentSkillRange())
            {
                Tile? tile = MapManager.Instance!.GetTile(pos.x, pos.y);
                if (tile != null)
                {
                    foreach (Enemy enemy in tile.EnemiesOnTile.ToList())
                    {
                        if (enemyIdSet.Add(enemy.GetInstanceID()))
                        {
                            // 코루틴은 Monobehaviour을 받는 객체에서만 실행 가능
                            // 이 스크립트는 ScriptableObject의 상속임. 실행 가능한 객체에게 요청한다.
                            caster.StartCoroutine(CreateMeteorSequence(caster, enemy));
                        }
                    }
                }
            }
            
            
        }

        private IEnumerator CreateMeteorSequence(Operator caster, Enemy target)
        {
            CreateMeteor(caster, target, meteorHeights.x);

            yield return new WaitForSeconds(meteorDelay);

            CreateMeteor(caster, target, meteorHeights.y);
        }

        private void CreateMeteor(Operator caster, Enemy target, float height)
        {
            if (target != null)
            {
                Vector3 spawnPos = target.transform.position + Vector3.up * height;
                GameObject meteorObj = ObjectPoolManager.Instance.SpawnFromPool(MeteorTag, spawnPos, Quaternion.identity, target.transform);

                MeteorController? controller = meteorObj.GetComponent<MeteorController>();

                if (controller != null)
                {
                    float actualDamage = caster.AttackPower * damageMultiplier;
                    controller.Initialize(caster, target, actualDamage, fallSpeed, stunDuration, hitVFXPrefab, HitVFXTag, MeteorTag);
                }
            }
        }

        // public string MeteorTag => _meteorTag ??= $"{skillName}_Meteor";
        // public string SkillRangeVFXTag => _skillRangeVFXTag ??= $"{skillName}_SkillRangeVFX";
        // public string HitVFXTag => _hitVFXTag ??= $"{skillName}_HitVFX";  

        public string MeteorTag
        {
            get
            {
                if (string.IsNullOrEmpty(_meteorTag))
                {
                    _meteorTag = $"{skillName}_Meteor";
                }
                return _meteorTag;
            }
        } 
        public string SkillRangeVFXTag
        {
            get
            {
                if (string.IsNullOrEmpty(_skillRangeVFXTag))
                {
                    _skillRangeVFXTag = $"{skillName}_SkillRangeVFX";
                }
                return _skillRangeVFXTag;
            }
        }        
        public string HitVFXTag
        {
            get
            {
                if (string.IsNullOrEmpty(_hitVFXTag))
                {
                    _hitVFXTag = $"{skillName}_HitVFX";
                }
                return _hitVFXTag;
            }
        }

        public override void PreloadObjectPools(OperatorData ownerData)
        {
            if (meteorPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(MeteorTag, meteorPrefab, 2);
            }
            if (skillRangeVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(SkillRangeVFXTag, skillRangeVFXPrefab, skillRangeOffset.Count);
            }
            if (hitVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(HitVFXTag, hitVFXPrefab, 10);
                // Logger.Log($"{HitVFXTag} 오브젝트 풀 생성 완료");
            }
        }

        protected void VisualizeSkillRange(Operator op, IReadOnlyCollection<Vector2Int> range)
        {
            foreach (Vector2Int pos in range)
            {
                if (MapManager.Instance!.CurrentMap!.IsValidGridPosition(pos.x, pos.y))
                {
                    Vector3 worldPos = MapManager.Instance!.ConvertToWorldPosition(pos);
                    GameObject? vfxObj = ObjectPoolManager.Instance?.SpawnFromPool(SkillRangeVFXTag, worldPos, Quaternion.identity);

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

        protected void VisualizeSkillRange(UnitEntity caster, IReadOnlyCollection<Vector2Int> range)
        {
            if (caster is Operator op)
            {
                VisualizeSkillRange(op, range);
            }
        }
    }
}

