using UnityEngine;
using System.Collections.Generic;

namespace Skills.Base
{
    [CreateAssetMenu(fileName = "New Area Haste Heal Skill", menuName = "Skills/Area Haste Heal Skill")]

    public class AreaHasteHealSkill : ActiveSkill
    {
        [Header("Field Settings")]
        [SerializeField] private GameObject fieldEffectPrefab = default!; // �� ���� ������
        [SerializeField] private GameObject hitEffectPrefab = default!;
        [SerializeField] private GameObject skillRangeVFXPrefab = default!;

        [Header("Skill Settings")]
        [SerializeField] private float healPerTickRatio = 0.7f;
        [SerializeField] private float healInterval = 0.5f;

        string FIELD_EFFECT_TAG; // ���� �ʵ� ȿ�� �±�
        string SKILL_RANGE_VFX_TAG; // �ʵ� ���� VFX �±�
        string HIT_EFFECT_TAG; // Ÿ�� ����Ʈ �±�

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

        // ���� ������ ����
        protected GameObject CreateHealField(Operator caster)
        {
            // �� ���� ������Ʈ ���� �� �ʱ�ȭ
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

        // �ð�ȭ ���� �޼��� (���� AreaEffectSkill�� ������ ������)
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
                        // �θ� op�� �����Ͽ� �����ֱ� ����ȭ
                        vfxObj.transform.SetParent(caster.transform);

                        var controller = vfxObj.GetComponent<SkillRangeVFXController>();
                        if (controller != null)
                        {
                            // duration ���� ǥ�õǵ��� ��Ʈ�ѷ� �ʱ�ȭ
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
