using UnityEngine;
namespace Skills.Base
{
    [CreateAssetMenu(fileName = "New Area Haste Heal Skill", menuName = "Skills/Area Haste Heal Skill")]

    public class AreaHasteHealSkill : AreaEffectSkill
    {
        [Header("Skill Settings")]
        [SerializeField] private float healPerTickRatio = 0.7f;
        [SerializeField] private float healInterval = 0.5f;

        // 공격 영역을 만듦
        protected override GameObject CreateEffectField(Operator op, Vector2Int centerPos)
        {
            GameObject fieldObj = Instantiate(fieldEffectPrefab);
            AreaHasteHealController controller = fieldObj.GetComponent<AreaHasteHealController>();

            if (controller != null && hitEffectPrefab != null)
            {
                float actualHealPerTick = op.AttackPower * healPerTickRatio;

                controller.Initialize(caster: op,
                    centerPosition: centerPos,
                    affectedTiles: actualSkillRange,
                    fieldDuration: duration,
                    amountPerTick: actualHealPerTick,
                    interval: healInterval,
                    hitEffectPrefab: hitEffectPrefab,
                    hitEffectTag: skillHitEffectTag
                );
            }

            return fieldObj;
        }


        protected override Vector2Int GetCenterPos(Operator op)
        {
            return MapManager.Instance!.ConvertToGridPosition(op.transform.position);
        }
    }
}
