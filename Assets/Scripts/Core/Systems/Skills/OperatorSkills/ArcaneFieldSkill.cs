
using UnityEngine;
using Skills.Base;
using System;
using System.Collections.Generic; // HashSet용

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
        [SerializeField] private float damagePerTickRatio = 0.7f; // 대미지 배율
        [SerializeField] private float slowAmount = 0.3f; // 이동속도 감소율
        [SerializeField] private float damageInterval = 0.5f; // 대미지 간격

        private CannotAttackBuff? _cannotAttackBuff;

        protected override void SetDefaults()
        {
            autoRecover = true;
        }

        protected override void PlaySkillEffect(Operator op)
        {
            // 1. 자신에게 공격 불가 버프 적용
            _cannotAttackBuff = new CannotAttackBuff(duration);
            op.AddBuff(_cannotAttackBuff);

            // 2. 장판과 VFX 생성
            UnitEntity? target = op.CurrentTarget;
            if (target == null) return;

            Vector2Int centerPos = MapManager.Instance!.ConvertToGridPosition(target.transform.position);

            caster = op;
            actualSkillRange.Clear();
            CalculateActualSkillRange(centerPos);

            // 실제 효과 장판 생성
            CreateEffectField(op);

            // 장판 시각적 효과 생성
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


            // 부모를 오퍼레이터로 설정해서 생명주기를 동기화한다
            // 즉 Operator가 파괴될 때 이 장판도 함께 파괴시키기 위한 것이라고 생각하면 됨
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

                    // 오브젝트 풀에서 VFX 객체를 가져옴
                    GameObject? vfxObj = ObjectPoolManager.Instance?.SpawnFromPool(vfxPoolTag, worldPos, Quaternion.identity);

                    if (vfxObj != null)
                    {
                        vfxObj.transform.SetParent(op.transform);

                        var controller = vfxObj.GetComponent<SkillRangeVFXController>();
                        if (controller != null)
                        {
                            // 실행 주기는 컨트롤러 자체에서 관리됨
                            controller.Initialize(pos, range, this.duration);
                        }
                    }
                }
            }
        }

        // 오브젝트 풀 초기화
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
