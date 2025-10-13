using UnityEngine;
using System.Collections.Generic;

namespace Skills.Base
{
    [CreateAssetMenu(fileName = "New Area Haste Heal Skill", menuName = "Skills/Area Haste Heal Skill")]

    public class AreaHasteHealSkill : ActiveSkill
    {
        [Header("Field Settings")]
        [SerializeField] private GameObject fieldEffectPrefab = default!; // 힐 장판 프리팹
        [SerializeField] private GameObject hitEffectPrefab = default!;
        [SerializeField] private GameObject skillRangeVFXPrefab = default!;

        [Header("Skill Settings")]
        [SerializeField] private float healPerTickRatio = 0.7f;
        [SerializeField] private float healInterval = 0.5f;

        string FIELD_EFFECT_TAG; // 실제 필드 효과 태그
        string SKILL_RANGE_VFX_TAG; // 필드 범위 VFX 태그
        string HIT_EFFECT_TAG; // 타격 이펙트 태그

        protected override void PlaySkillEffect(Operator caster)
        {
            if (hitEffectPrefab == null)
            {
                hitEffectPrefab = caster.OperatorData.HitEffectPrefab;
            }

            caster.AddBuff(new CannotAttackBuff(duration, this));

            Vector2Int centerPos = GetCenterGridPos(caster);
            if (centerPos != caster.LastSkillCenter)
            {
                HashSet<Vector2Int> skillRange = PositionCalculationSystem.CalculateRange(skillRangeOffset, centerPos, caster.FacingDirection);
                caster.SetCurrentSkillRange(skillRange);
            }

            CreateHealField(caster);

            VisualizeSkillRange(caster, caster.GetCurrentSkillRange());
        }

        protected override void OnSkillEnd(Operator caster)
        {
            caster.RemoveBuffFromSourceSkill(this);
        
            base.OnSkillEnd(caster);
        }

        // 공격 영역을 만듦
        protected GameObject CreateHealField(Operator caster)
        {
            // 힐 장판 오브젝트 생성 및 초기화
            GameObject fieldObj = Instantiate(fieldEffectPrefab, caster.transform);
            AreaHasteHealController? controller = fieldObj.GetComponent<AreaHasteHealController>();

            if (controller != null && hitEffectPrefab != null)
            {   
                controller.Initialize(caster: caster,
                    skillRangeGridPositions: caster.GetCurrentSkillRange(),
                    fieldDuration: duration,
                    tickDamageRatio: healPerTickRatio,
                    interval: healInterval,
                    hitEffectPrefab: hitEffectPrefab,
                    hitEffectTag: HIT_EFFECT_TAG
                );
            }

            return fieldObj;
        }

        // 시각화 헬퍼 메서드 (이전 AreaEffectSkill의 로직을 가져옴)
        private void VisualizeSkillRange(Operator caster, IReadOnlyCollection<Vector2Int> range)
        {
            foreach (Vector2Int pos in range)
            {
                if (MapManager.Instance!.CurrentMap!.IsValidGridPosition(pos.x, pos.y))
                {
                    Vector3 worldPos = MapManager.Instance!.ConvertToWorldPosition(pos);
                    GameObject? vfxObj = ObjectPoolManager.Instance?.SpawnFromPool(SKILL_RANGE_VFX_TAG, worldPos, Quaternion.identity);

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

        public override void PreloadObjectPools(OperatorData ownerData)
        {
            base.PreloadObjectPools(ownerData);

            if (fieldEffectPrefab != null)
            {
                FIELD_EFFECT_TAG = RegisterPool(ownerData, fieldEffectPrefab, 2);
            }
            if (skillRangeVFXPrefab != null)
            {
                SKILL_RANGE_VFX_TAG = RegisterPool(ownerData, skillRangeVFXPrefab, skillRangeOffset.Count);
            }
            if (hitEffectPrefab != null)
            {
                HIT_EFFECT_TAG = RegisterPool(ownerData, hitEffectPrefab, 10);
            }
        }
    }
}
