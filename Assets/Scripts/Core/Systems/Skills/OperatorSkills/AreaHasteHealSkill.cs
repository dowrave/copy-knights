using UnityEngine;
using System.Collections.Generic;

namespace Skills.Base
{
    [CreateAssetMenu(fileName = "New Area Haste Heal Skill", menuName = "Skills/Area Haste Heal Skill")]

    public class AreaHasteHealSkill : ActiveSkill
    {
        [Header("Field Settings")]
        [SerializeField] private GameObject skillControllerPrefab = default!; // 힐 장판 프리팹
        [SerializeField] private GameObject hitVFXPrefab = default!;
        [SerializeField] private GameObject skillRangeVFXPrefab = default!;

        [Header("Skill Settings")]
        [SerializeField] private float healPerTickRatio = 0.7f;
        [SerializeField] private float healInterval = 0.5f;

        private string _skillControllerTag;
        private string _skillRangeVFXTag;
        private string _hitVFXTag;

        public override void OnActivated(Operator caster)
        {
            if (hitVFXPrefab == null)
            {
                hitVFXPrefab = caster.OperatorData.HitEffectPrefab;
            }

            caster.AddBuff(new CannotAttackBuff(duration, this));

            Vector2Int centerPos = GetCenterGridPos(caster);
            if (centerPos != caster.LastSkillCenter)
            {
                HashSet<Vector2Int> skillRange = PositionCalculationSystem.CalculateRange(skillRangeOffset, centerPos, caster.FacingDirection.Value);
                caster.SetCurrentSkillRange(skillRange);
            }

            CreateHealField(caster);

            VisualizeSkillRange(caster, caster.GetCurrentSkillRange());
        }

        public override void OnEnd(Operator caster)
        {
            caster.RemoveBuffFromSourceSkill(this);
        }

        // 공격 영역을 만듦
        protected GameObject CreateHealField(Operator caster)
        {
            // 힐 장판 오브젝트 생성 및 초기화
            // 오브젝트 풀링 기반으로 수정할 수 있을 듯
            // GameObject fieldObj = Instantiate(skillControllerPrefab, caster.transform);
            GameObject skillControllerObj = ObjectPoolManager.Instance!.SpawnFromPool(
                SkillControllerTag,
                caster.transform.position,
                Quaternion.identity
            );
            AreaHasteHealController? controller = skillControllerObj.GetComponent<AreaHasteHealController>();

            if (controller != null && hitVFXPrefab != null)
            {
                controller.Initialize(caster: caster,
                    skillRangeGridPositions: caster.GetCurrentSkillRange(),
                    fieldDuration: duration,
                    tickDamageRatio: healPerTickRatio,
                    interval: healInterval,
                    hitEffectPrefab: hitVFXPrefab,
                    hitEffectTag: HitVFXTag
                );
            }

            return skillControllerObj;
        }

        // 시각화 헬퍼 메서드 (이전 AreaEffectSkill의 로직을 가져옴)
        private void VisualizeSkillRange(Operator caster, IReadOnlyCollection<Vector2Int> range)
        {
            foreach (Vector2Int pos in range)
            {
                if (MapManager.Instance!.CurrentMap!.IsValidGridPosition(pos.x, pos.y))
                {
                    Vector3 worldPos = MapManager.Instance!.ConvertToWorldPosition(pos);
                    GameObject? vfxObj = ObjectPoolManager.Instance?.SpawnFromPool(SkillRangeVFXTag, worldPos, Quaternion.identity);

                    if (vfxObj != null)
                    {
                        // 부모를 op로 설정하여 생명주기 동기화
                        vfxObj.transform.SetParent(caster.transform);

                        var controller = vfxObj.GetComponent<SkillRangeVFXController>();
                        if (controller != null)
                        {
                            // duration 동안 표시되도록 컨트롤러 초기화
                            controller.Initialize(pos, range, this.duration);
                        }
                    }
                }
            }
        }

        public string SkillControllerTag
        {
            get
            {
                if (string.IsNullOrEmpty(_skillControllerTag))
                {
                    _skillControllerTag = $"{skillName}_SkillController";
                }
                return _skillControllerTag;
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
            base.PreloadObjectPools(ownerData);

            if (skillControllerPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(SkillControllerTag, skillControllerPrefab, 1);
                // FIELD_EFFECT_TAG = RegisterPool(ownerData, fieldEffectPrefab, 2);
            }
            if (skillRangeVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(SkillRangeVFXTag, skillRangeVFXPrefab, skillRangeOffset.Count);
                // SKILL_RANGE_VFX_TAG = RegisterPool(ownerData, skillRangeVFXPrefab, skillRangeOffset.Count);
            }
            if (hitVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(HitVFXTag, hitVFXPrefab, 10);
                // HIT_EFFECT_TAG = RegisterPool(ownerData, hitEffectPrefab, 10);
            }
        }
    }
}
