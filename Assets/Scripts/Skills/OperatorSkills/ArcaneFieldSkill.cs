
using UnityEngine;
using Skills.Base;
using System;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Arcane Field Skill", menuName = "Skills/Arcane Field Skill")]
    public class ArcaneFieldSkill : AreaEffectSkill
    {
        [Header("Damage Settings")]
        [SerializeField] private float damagePerTickRatio = 0.7f; // 대미지 배율
        [SerializeField] private float slowAmount = 0.3f; // 이동속도 감소율
        [SerializeField] private float damageInterval = 0.5f; // 대미지 간격

        public override void Activate(Operator op)
        {
            mainTarget = op.CurrentTarget as Enemy;
            if (mainTarget == null) return; // 타겟이 없을 때는 스킬 취소

            base.Activate(op);
        }

        protected override GameObject CreateEffectField(Operator op, Vector2Int centerPos)
        {
            GameObject fieldObj = Instantiate(fieldEffectPrefab);
            ArcaneFieldController? controller = fieldObj.GetComponent<ArcaneFieldController>(); 

            if (controller != null)
            {
                float actualDamagePerTick = op.AttackPower * damagePerTickRatio;
                controller.Initialize(
                    op,
                    centerPos,
                    actualSkillRange,
                    duration,
                    actualDamagePerTick,
                    damageInterval,
                    hitEffectPrefab!,
                    skillHitEffectTag,
                    slowAmount
                );
            }

            return fieldObj;
        }

        protected override Vector2Int GetCenterPos(Operator op)
        {
            if (mainTarget == null) throw new InvalidOperationException("mainTarget이 null임");
            // mainTarget을 중심으로 시전되므로
            return MapManager.Instance!.ConvertToGridPosition(mainTarget.transform.position);
        }
    }
}
