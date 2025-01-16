
using UnityEngine;
using Skills.Base;
using System.Collections.Generic;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Arcane Field Skill", menuName = "Skills/Arcane Field Skill")]
    public class ArcaneFieldSkill : AreaEffectSkill
    {
        [Header("Damage Settings")]
        [SerializeField] private float damagePerTickRatio = 0.7f; // 대미지 배율
        [SerializeField] private float slowAmount = 0.3f; // 이동속도 감소율
        [SerializeField] private float damageInterval = 0.5f; // 대미지 간격

        protected override GameObject CreateEffectField(Operator op, Vector2Int centerPos)
        {
            GameObject fieldObj = Instantiate(fieldEffectPrefab);
            ArcaneFieldController controller = fieldObj.GetComponent<ArcaneFieldController>(); 

            if (controller != null)
            {
                float actualDamagePerTick = op.AttackPower * damagePerTickRatio;
                controller.Initialize(op, centerPos, actualSkillRange, actualDamagePerTick, slowAmount, duration, damageInterval);
            }

            return fieldObj;
        }
    }
}
