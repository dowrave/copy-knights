
using UnityEngine;
using Skills.Base;
using System;
using System.Collections.Generic; // HashSet��

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Arcane Field Skill", menuName = "Skills/Arcane Field Skill")]
    public class ArcaneFieldSkill : ActiveSkill
    {
        [Header("Field Settings")]
        [SerializeField] private GameObject fieldEffectPrefab = default!;
        [SerializeField] private GameObject skillRangeVFXPrefab = default!;
        [SerializeField] private GameObject hitEffectPrefab = default!;
        private string hitEffectTag = "ArcaneHit";

        [Header("Damage Settings")]
        [SerializeField] private float damagePerTickRatio = 0.7f; // ����� ����
        [SerializeField] private float slowAmount = 0.3f; // �̵��ӵ� ������
        [SerializeField] private float damageInterval = 0.5f; // ����� ����

        private CannotAttackBuff? _cannotAttackBuff;

        protected override void SetDefaults()
        {
            autoRecover = true;
        }

        protected override void PlaySkillEffect(Operator op)
        {
            // 1. �ڽſ��� ���� �Ұ� ���� ����
            _cannotAttackBuff = new CannotAttackBuff(duration);
            op.AddBuff(_cannotAttackBuff);

            // 2. ���ǰ� VFX ����
            UnitEntity? target = op.CurrentTarget;
            if (target == null) return;

            Vector2Int centerPos = MapManager.Instance!.ConvertToGridPosition(target.transform.position);

            caster = op;
            actualSkillRange.Clear();
            CalculateActualSkillRange(centerPos);

            // ���� ȿ�� ���� ����
            CreateEffectField(op);

            // ���� �ð��� ȿ�� ����
            VisualizeSkillRange(op, actualSkillRange);

            base.PlaySkillEffect(op);
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


        protected void CreateEffectField(Operator op)
        {
            if (hitEffectPrefab == null)
            {
                hitEffectPrefab = op.OperatorData.HitEffectPrefab;
            }


            // �θ� ���۷����ͷ� �����ؼ� �����ֱ⸦ ����ȭ�Ѵ�
            // �� Operator�� �ı��� �� �� ���ǵ� �Բ� �ı���Ű�� ���� ���̶�� �����ϸ� ��
            GameObject fieldObj = Instantiate(fieldEffectPrefab, op.transform);
            ArcaneFieldController? controller = fieldObj.GetComponent<ArcaneFieldController>();

            if (controller != null)
            {
                float actualDamagePerTick = op.AttackPower * damagePerTickRatio;
                controller.Initialize(
                    op,
                    actualSkillRange,
                    duration,
                    actualDamagePerTick,
                    damageInterval,
                    hitEffectPrefab!,
                    hitEffectTag,
                    slowAmount
                );
            }
        }

        private void VisualizeSkillRange(Operator op, HashSet<Vector2Int> range)
        {
            string vfxPoolTag = $"{this.name}_RangeVFX";

            foreach (Vector2Int pos in range)
            {
                if (MapManager.Instance!.CurrentMap!.IsValidGridPosition(pos.x, pos.y))
                {
                    Vector3 worldPos = MapManager.Instance!.ConvertToWorldPosition(pos);

                    // ������Ʈ Ǯ���� VFX ��ü�� ������
                    GameObject? vfxObj = ObjectPoolManager.Instance?.SpawnFromPool(vfxPoolTag, worldPos, Quaternion.identity);

                    if (vfxObj != null)
                    {
                        vfxObj.transform.SetParent(op.transform);

                        var controller = vfxObj.GetComponent<SkillRangeVFXController>();
                        if (controller != null)
                        {
                            // ���� �ֱ�� ��Ʈ�ѷ� ��ü���� ������
                            controller.Initialize(pos, range, this.duration);
                        }
                    }
                }
            }
        }

        // ������Ʈ Ǯ �ʱ�ȭ
        public override void InitializeSkillObjectPool(UnitEntity caster)
        {
            base.InitializeSkillObjectPool(caster);
            if (caster is Operator op)
            {
                if (fieldEffectPrefab != null) ObjectPoolManager.Instance!.CreatePool($"{op.OperatorData.entityName}_{this.name}_Field", fieldEffectPrefab, 2);
                if (skillRangeVFXPrefab != null) ObjectPoolManager.Instance!.CreatePool($"{op.OperatorData.entityName}_{this.name}_RangeVFX", skillRangeVFXPrefab, skillRangeOffset.Count);
                if (hitEffectPrefab != null){ ObjectPoolManager.Instance!.CreatePool(hitEffectTag, hitEffectPrefab, 10); }
            }
        }
    }
}
