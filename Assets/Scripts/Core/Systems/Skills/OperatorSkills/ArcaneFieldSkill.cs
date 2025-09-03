
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

        [Header("Damage Settings")]
        [SerializeField] private float damagePerTickRatio = 0.7f; // ����� ����
        [SerializeField] private float slowAmount = 0.3f; // �̵��ӵ� ������
        [SerializeField] private float damageInterval = 0.5f; // ����� ����

        // private CannotAttackBuff? _cannotAttackBuff;

        string FIELD_EFFECT_TAG; // ���� �ʵ� ȿ�� �±�
        string SKILL_RANGE_VFX_TAG; // �ʵ� ���� VFX �±�
        string HIT_EFFECT_TAG; // Ÿ�� ����Ʈ �±�

        protected override void SetDefaults()
        {
            autoRecover = true;
        }

        protected override void PlaySkillEffect(Operator caster)
        {
            // 1. �ڽſ��� ���� �Ұ� ���� ����
            CannotAttackBuff _cannotAttackBuff = new CannotAttackBuff(duration, this);
            caster.AddBuff(_cannotAttackBuff);

            // 2. ���ǰ� VFX ����
            UnitEntity? target = caster.CurrentTarget;
            if (target == null) return;

            // ��ǥ�� �߽����� ������ ���
            Vector2Int centerPos = MapManager.Instance!.ConvertToGridPosition(target.transform.position);
            if (caster.LastSkillCenter != centerPos)
            {
                HashSet<Vector2Int> skillRange = PositionCalculationSystem.CalculateRange(skillRangeOffset, centerPos, caster.FacingDirection);
                caster.SetCurrentSkillRange(skillRange);
                caster.SetLastSkillCenter(centerPos);
            }

            // ���� ȿ�� ���� ����
            CreateEffectField(caster);

            // ���� �ð��� ȿ�� ����
            VisualizeSkillRange(caster, caster.GetCurrentSkillRange());

            base.PlaySkillEffect(caster);
        }

        protected override void OnSkillEnd(Operator caster)
        {
            caster.RemoveBuffFromSourceSkill(this);

            base.OnSkillEnd(caster);
        }


        protected void CreateEffectField(Operator caster)
        {
            if (hitEffectPrefab == null)
            {
                hitEffectPrefab = caster.OperatorData.HitEffectPrefab;
            }


            // �θ� ���۷����ͷ� �����ؼ� �����ֱ⸦ ����ȭ�Ѵ�
            // �� Operator�� �ı��� �� �� ���ǵ� �Բ� �ı���Ű�� ���� ���̶�� �����ϸ� ��
            GameObject fieldObj = Instantiate(fieldEffectPrefab, caster.transform);
            ArcaneFieldController? controller = fieldObj.GetComponent<ArcaneFieldController>();

            if (controller != null)
            {
                controller.Initialize(
                    caster,
                    caster.GetCurrentSkillRange(),
                    duration,
                    damagePerTickRatio,
                    damageInterval,
                    hitEffectPrefab!,
                    HIT_EFFECT_TAG,
                    slowAmount
                );
            }
        }

        private void VisualizeSkillRange(Operator caster, IReadOnlyCollection<Vector2Int> range)
        {
            foreach (Vector2Int pos in range)
            {
                if (MapManager.Instance!.CurrentMap!.IsValidGridPosition(pos.x, pos.y))
                {
                    Vector3 worldPos = MapManager.Instance!.ConvertToWorldPosition(pos);

                    // ������Ʈ Ǯ���� VFX ��ü�� ������
                    GameObject? vfxObj = ObjectPoolManager.Instance?.SpawnFromPool(SKILL_RANGE_VFX_TAG, worldPos, Quaternion.identity);

                    if (vfxObj != null)
                    {
                        vfxObj.transform.SetParent(caster.transform);

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

        // ������Ʈ Ǯ �ʱ�ȭ.
        // ���۷����Ͷ�� ��ġ�Ǵ� ������ ����ȴ�. 
        public override void InitializeSkillObjectPool(UnitEntity caster)
        {
            base.InitializeSkillObjectPool(caster);

            if (caster is Operator op)
            {
                if (fieldEffectPrefab != null)
                {
                    FIELD_EFFECT_TAG = RegisterPool(op, fieldEffectPrefab, 2);
                }
                if (skillRangeVFXPrefab != null)
                {
                    SKILL_RANGE_VFX_TAG = RegisterPool(op, skillRangeVFXPrefab, skillRangeOffset.Count);
                }
                if (hitEffectPrefab != null)
                {
                    HIT_EFFECT_TAG = RegisterPool(op, hitEffectPrefab, 10);
                }
            }
        }
    }
}
