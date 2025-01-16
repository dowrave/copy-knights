
using System.Collections.Generic;
using UnityEngine;

namespace Skills.Base
{
    [CreateAssetMenu(fileName = "New Meteor Skill", menuName = "Skills/Meteor Skill")]
    public class MeteorSkill : AreaEffectSkill, IEffectController
    {
        [Header("Skill Settings")]
        [SerializeField] private float damageMultiplier = 0.5f;
        [SerializeField] private float stunDuration = 2f;
        [SerializeField] private int costRecovery = 10;

        [SerializeField] private GameObject meteorEffectPrefab;

        protected override GameObject CreateEffectField(Operator op, Vector2Int centerPos)
        {
            GameObject fieldObj = Instantiate(fieldEffectPrefab);

            return fieldObj;
            //ArcaneFieldController controller = fieldObj.GetComponent<ArcaneFieldController>();

            //if (controller != null)
            //{
            //    float actualDamagePerTick = op.AttackPower * damagePerTickRatio;
            //    controller.Initialize(op, centerPos, actualSkillRange, actualDamagePerTick, slowAmount, duration, damageInterval);
            //}
        }

        public void ForceRemove()
        {

        }
    }
}

