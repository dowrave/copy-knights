
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
        [SerializeField] private GameObject skillControllerPrefab = default!;
        [SerializeField] private GameObject skillRangeVFXPrefab = default!;
        [SerializeField] private GameObject hitVFXPrefab = default!;

        [Header("Damage Settings")]
        [SerializeField] private float damagePerTickRatio = 0.7f; // 대미지 배율
        [SerializeField] private float slowAmount = 0.3f; // 이동속도 감소율
        [SerializeField] private float damageInterval = 0.5f; // 대미지 간격
        
        protected override void SetDefaults()
        {
            autoRecover = true;
        }

        protected override void PlaySkillEffect(Operator caster)
        {
            // 1. 자신에게 공격 불가 버프 적용
            CannotAttackBuff _cannotAttackBuff = new CannotAttackBuff(duration, this);
            caster.AddBuff(_cannotAttackBuff);

            // 2. 장판과 VFX 생성
            UnitEntity? target = caster.CurrentTarget;
            if (target == null) return;

            // 목표를 중심으로 범위를 계산
            Vector2Int centerPos = MapManager.Instance!.ConvertToGridPosition(target.transform.position);
            if (caster.LastSkillCenter != centerPos)
            {
                HashSet<Vector2Int> skillRange = PositionCalculationSystem.CalculateRange(skillRangeOffset, centerPos, caster.FacingDirection);
                caster.SetCurrentSkillRange(skillRange);
                caster.SetLastSkillCenter(centerPos);
            }

            // 실제 효과 장판 생성
            CreateEffectField(caster);

            // 장판 시각적 효과 생성
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
            // 부모를 오퍼레이터로 설정해서 생명주기를 동기화한다
            // 즉 Operator가 파괴될 때 이 장판도 함께 파괴시키기 위한 것이라고 생각하면 됨
            GameObject skillControllerObj = ObjectPoolManager.Instance!.SpawnFromPool(
                GetSkillControllerTag(caster.OperatorData),
                caster.transform.position,
                Quaternion.identity
            );
            
            ArcaneFieldController? controller = skillControllerObj.GetComponent<ArcaneFieldController>();

            if (controller != null)
            {
                controller.Initialize(
                    caster,
                    caster.GetCurrentSkillRange(),
                    duration,
                    damagePerTickRatio,
                    damageInterval,
                    hitVFXPrefab!,
                    GetHitVFXTag(caster.OperatorData),
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

                    // 오브젝트 풀에서 VFX 객체를 가져옴
                    GameObject? vfxObj = ObjectPoolManager.Instance?.SpawnFromPool(GetSkillRangeVFXTag(caster.OperatorData), worldPos, Quaternion.identity);

                    if (vfxObj != null)
                    {
                        vfxObj.transform.SetParent(caster.transform);

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

        public string GetSkillControllerTag(OperatorData ownerData) => $"{ownerData}_{skillName}_SkillController";
        public string GetSkillRangeVFXTag(OperatorData ownerData) => $"{ownerData}_{skillName}_SkillRangeVFX";
        public string GetHitVFXTag(OperatorData ownerData) => $"{ownerData}_{skillName}_HitVFX";

        // 스킬 관련 오브젝트 풀 초기화.
        public override void PreloadObjectPools(OperatorData ownerData)
        {
            base.PreloadObjectPools(ownerData);

            if (skillControllerPrefab != null) ObjectPoolManager.Instance.CreatePool(GetSkillControllerTag(ownerData), skillControllerPrefab, 1);
            if (skillRangeVFXPrefab != null) ObjectPoolManager.Instance.CreatePool(GetSkillRangeVFXTag(ownerData), skillRangeVFXPrefab, skillRangeOffset.Count);
            if (hitVFXPrefab != null) ObjectPoolManager.Instance.CreatePool(GetHitVFXTag(ownerData), hitVFXPrefab, 10);
        }
        
    }
}
