using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Skills.Base
{
    public class AreaHasteHealSkill : AreaEffectSkill
    {
        [Header("Skill Settings")]
        [SerializeField] private float healPerTickRatio = 0.7f;
        [SerializeField] private float healInterval = 0.5f;

        protected override GameObject CreateEffectField(Operator op, Vector2Int centerPos)
        {
            GameObject fieldObj = Instantiate(fieldEffectPrefab);
            AreaHasteHealController controller = fieldObj.GetComponent<AreaHasteHealController>();

            if (controller != null)
            {
                float actualHealPerTick = op.AttackPower * healPerTickRatio;
                controller.Initialize(op, centerPos, actualSkillRange, duration, actualHealPerTick, healInterval);
            }

            return fieldObj;
        }


        protected override Vector2Int GetCenterPos(Operator op)
        {
            // mainTarget을 중심으로 시전되므로
            return MapManager.Instance.ConvertToGridPosition(mainTarget.transform.position);
        }
    }
}
