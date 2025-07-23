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

        private CannotAttackBuff? _cannotAttackBuff;

        string FIELD_EFFECT_TAG; // 실제 필드 효과 태그
        string SKILL_RANGE_VFX_TAG; // 필드 범위 VFX 태그
        string HIT_EFFECT_TAG; // 타격 이펙트 태그

        protected override void PlaySkillEffect(Operator op)
        {
            if (hitEffectPrefab == null)
            {
                hitEffectPrefab = op.OperatorData.HitEffectPrefab;
            }

            _cannotAttackBuff = new CannotAttackBuff(this.duration);
            op.AddBuff(_cannotAttackBuff);

            caster = op;
            Vector2Int centerPos = MapManager.Instance!.ConvertToGridPosition(op.transform.position);
            actualSkillRange.Clear();
            CalculateActualSkillRange(centerPos);

            CreateHealField(op);

            VisualizeSkillRange(op, actualSkillRange);
        }

        protected override void OnSkillEnd(Operator op)
        {
            if (_cannotAttackBuff != null)
            {
                op.RemoveBuff(_cannotAttackBuff);
                _cannotAttackBuff = null;
            }
            base.OnSkillEnd(op);
        }

        // 공격 영역을 만듦
        protected GameObject CreateHealField(Operator op)
        {
            // 범위 계산
            caster = op;
            Vector2Int centerPos = MapManager.Instance!.ConvertToGridPosition(op.transform.position);
            actualSkillRange.Clear();
            CalculateActualSkillRange(centerPos);

            // 힐 장판 오브젝트 생성 및 초기화
            GameObject fieldObj = Instantiate(fieldEffectPrefab, op.transform);
            AreaHasteHealController? controller = fieldObj.GetComponent<AreaHasteHealController>();

            if (controller != null && hitEffectPrefab != null)
            {
                float actualHealPerTick = op.AttackPower * healPerTickRatio;
                
                controller.Initialize(caster: op,
                    affectedTiles: actualSkillRange,
                    fieldDuration: duration,
                    amountPerTick: actualHealPerTick,
                    interval: healInterval,
                    hitEffectPrefab: hitEffectPrefab,
                    hitEffectTag: HIT_EFFECT_TAG
                );
            }

            return fieldObj;
        }

        // 시각화 헬퍼 메서드 (이전 AreaEffectSkill의 로직을 가져옴)
        private void VisualizeSkillRange(Operator op, HashSet<Vector2Int> range)
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
                        vfxObj.transform.SetParent(op.transform);

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

        public override void InitializeSkillObjectPool(UnitEntity caster)
        {
            base.InitializeSkillObjectPool(caster);
            if (caster is Operator op)
            {
                if (fieldEffectPrefab != null)
                {
                    FIELD_EFFECT_TAG = RegisterPool(op, fieldEffectPrefab, 2);
                }
                if (skillRangeVFXPrefab != null)
                {
                    SKILL_RANGE_VFX_TAG = RegisterPool(op, skillRangeVFXPrefab, skillRangeOffset.Count);
                }
                if (hitEffectPrefab != null)
                {
                    HIT_EFFECT_TAG = RegisterPool(op, hitEffectPrefab, 10);
                }
            }
        }
    }
}
