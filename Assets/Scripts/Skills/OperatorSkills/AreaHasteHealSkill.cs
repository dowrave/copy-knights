using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
namespace Skills.Base
{
    [CreateAssetMenu(fileName = "New Area Haste Heal Skill", menuName = "Skills/Area Haste Heal Skill")]

    public class AreaHasteHealSkill : AreaEffectSkill
    {
        [Header("Skill Settings")]
        [SerializeField] private float healPerTickRatio = 0.7f;
        [SerializeField] private float healInterval = 0.5f;

        protected override GameObject CreateEffectField(Operator op, Vector2Int centerPos)
        {
            GameObject fieldObj = Instantiate(fieldEffectPrefab);
            AreaHasteHealController controller = fieldObj.GetComponent<AreaHasteHealController>();

            if (controller != null && hitEffectPrefab != null)
            {
                float actualHealPerTick = op.AttackPower * healPerTickRatio;
                controller.Initialize(op, centerPos, actualSkillRange, duration, actualHealPerTick, healInterval, hitEffectPrefab, skillHitEffectTag);
            }

            return fieldObj;
        }


        protected override Vector2Int GetCenterPos(Operator op)
        {
            return MapManager.Instance!.ConvertToGridPosition(op.transform.position);
        }
    }
}
