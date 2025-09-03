using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Skills.Base
{
    [CreateAssetMenu(fileName = "Boss Explosion Skill", menuName = "Skills/Boss/Explosion Skill")]
    public class BossExplosionSkill : EnemyBossSkill
    {
        [Header("Skill Configs")]
        [SerializeField] private float castTime = 0.5f; // 스킬 시전 시간
        [SerializeField] private float fallDuration = 3f; // 해 파티클 떨어지는 시간
        [SerializeField] private float lingeringDuration = 5f; // 지속 피해 유지 시간
        [SerializeField] private float damageInterval = 0.5f; // 지속 피해 간격
        [SerializeField] private float tickDamageRatio = 0.5f; // 지속 피해 배율

        [Header("Actual Effect")]
        // [SerializeField]
        [SerializeField] private List<Vector2Int> rangeOffset = new List<Vector2Int>();
        [SerializeField] private float damageMultiplier = 3f;
        [SerializeField] private float dotDamageMultiplier = 0.3f;

        [Header("SkillController Prefab")]
        [SerializeField] private GameObject skillControllerPrefab = default!;

        [Header("VFX")]
        [SerializeField] private GameObject castVFXPrefab = default!;
        [SerializeField] private GameObject areaVFXPrefab = default!;
        [SerializeField] private GameObject fallingSunVFXPrefab = default!;
        [SerializeField] private GameObject explosionVFXPrefab = default!;

        // 여기 구현하는 게 바람직하지 않지만 일단 원래 구현 따라간 다음에 수정함
        private string CAST_VFX_TAG = string.Empty;
        private string AREA_VFX_TAG = string.Empty;
        private string SUN_VFX_TAG = string.Empty;
        private string EXPLOSION_VFX_TAG = string.Empty;
        private string SKILL_EFFECT_TAG = string.Empty;


        public override void Activate(EnemyBoss caster)
        {
            UnitEntity target = caster.CurrentTarget; // 수정 필요할 수 있음

            if (target == null) return;

            IEnumerator sequence = ActivateSequence(caster, target);
            caster.ExecuteSkillSequence(sequence);
        }
        
        public IEnumerator ActivateSequence(EnemyBoss caster, UnitEntity target)
        {
             // 범위 설정
            Vector2Int centerPos = MapManager.Instance!.ConvertToGridPosition(target.transform.position);
            if (caster.LastSkillCenter != centerPos)
            {
                HashSet<Vector2Int> skillRange = PositionCalculationSystem.CalculateRange(rangeOffset, centerPos, caster.transform.forward); // 방향은 크게 상관 없음
                caster.SetCurrentSkillRange(skillRange);
                caster.SetLastSkillCenter(centerPos);
            }

            // 범위를 기반으로 스킬 컨트롤러 생성, 스킬의 재생은 모두 여기서 담당함
            GameObject fieldObj = Instantiate(skillControllerPrefab, caster.transform);
            BossRangedSkillController? controller = fieldObj.GetComponent<BossRangedSkillController>();

            if (controller != null)
            {
                // Caster 위치에 Cast 이펙트를 실행함
                GameObject castVFXObject = Instantiate(castVFXPrefab, caster.gameObject.transform);
                ParticleSystem castVFX = castVFXObject.GetComponent<ParticleSystem>();
                if (castVFX != null)
                {
                    castVFX.Play(true);
                }

                // 시전 동작 중에는 대기 
                yield return new WaitForSeconds(castTime);

                controller.Initialize(
                    caster: caster,
                    skillRangeGridPositions: caster.GetCurrentSkillRange(),
                    fieldDuration: castTime,
                    tickDamageRatio: tickDamageRatio,
                    interval: damageInterval,
                    castTime: castTime,
                    fallDuration: fallDuration,
                    lingeringDuration: lingeringDuration,
                    hitEffectPrefab: caster.BaseData.HitEffectPrefab, // 임시
                    hitEffectTag: $"{caster.BaseData.entityName}_{skillName}"
                );
            }
        }

        public override void InitializeSkillObjectPool(UnitEntity caster)
        {
            base.InitializeSkillObjectPool(caster);
        }
    }
}