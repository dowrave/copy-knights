
using UnityEngine;
using Skills.Base;
using System;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Arcane Field Skill", menuName = "Skills/Arcane Field Skill")]
    public class ArcaneFieldSkill : AreaEffectSkill
    {
        [Header("Damage Settings")]
        [SerializeField] private float damagePerTickRatio = 0.7f; // ����� ����
        [SerializeField] private float slowAmount = 0.3f; // �̵��ӵ� ������
        [SerializeField] private float damageInterval = 0.5f; // ����� ����

        public override void Activate(Operator op)
        {
            mainTarget = op.CurrentTarget as Enemy;
            if (mainTarget == null) return; // Ÿ���� ���� ���� ��ų ���

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
            if (mainTarget == null) throw new InvalidOperationException("mainTarget�� null��");
            // mainTarget�� �߽����� �����ǹǷ�
            return MapManager.Instance!.ConvertToGridPosition(mainTarget.transform.position);
        }
    }
}
