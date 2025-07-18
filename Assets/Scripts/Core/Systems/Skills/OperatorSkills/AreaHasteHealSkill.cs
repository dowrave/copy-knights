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
        string hitEffectTag = "AreaHealHit";

        [Header("Skill Settings")]
        [SerializeField] private float healPerTickRatio = 0.7f;
        [SerializeField] private float healInterval = 0.5f;

        private CannotAttackBuff? _cannotAttackBuff;

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

        // ���� ������ ����
        protected GameObject CreateHealField(Operator op)
        {
            // ���� ���
            caster = op;
            Vector2Int centerPos = MapManager.Instance!.ConvertToGridPosition(op.transform.position);
            actualSkillRange.Clear();
            CalculateActualSkillRange(centerPos);

            // �� ���� ������Ʈ ���� �� �ʱ�ȭ
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
                    hitEffectTag: hitEffectTag
                );
            }

            return fieldObj;
        }

        // �ð�ȭ ���� �޼��� (���� AreaEffectSkill�� ������ ������)
        private void VisualizeSkillRange(Operator op, HashSet<Vector2Int> range)
        {
            string vfxPoolTag = $"{this.name}_RangeVFX";

            foreach (Vector2Int pos in range)
            {
                if (MapManager.Instance!.CurrentMap!.IsValidGridPosition(pos.x, pos.y))
                {
                    Vector3 worldPos = MapManager.Instance!.ConvertToWorldPosition(pos);
                    GameObject? vfxObj = ObjectPoolManager.Instance?.SpawnFromPool(vfxPoolTag, worldPos, Quaternion.identity);

                    if (vfxObj != null)
                    {
                        // �θ� op�� �����Ͽ� �����ֱ� ����ȭ
                        vfxObj.transform.SetParent(op.transform);

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

        public override void InitializeSkillObjectPool()
        {
            if (fieldEffectPrefab != null) ObjectPoolManager.Instance!.CreatePool($"{this.name}_Field", fieldEffectPrefab, 2);
            if (hitEffectPrefab != null) ObjectPoolManager.Instance!.CreatePool($"{this.name}_HitEffect", hitEffectPrefab, 10);
            if (skillRangeVFXPrefab != null) ObjectPoolManager.Instance!.CreatePool($"{this.name}_RangeVFX", skillRangeVFXPrefab, skillRangeOffset.Count);
        }
    }
}
