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
        [SerializeField] private float explosionDamageRatio = 3f;
        [SerializeField] private float lingeringDuration = 5f; // 지속 피해 유지 시간
        [SerializeField] private float tickInterval = 0.5f; // 지속 피해 간격
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
        [SerializeField] private GameObject hitVFXPrefab = default!; // 타격 시 적에게 나타날 이펙트 프리팹
        [SerializeField] private GameObject fallingSunVFXPrefab = default!; // 공격 범위 VFX
        [SerializeField] private GameObject skillRangeVFXPrefab = default!; // 공격 범위 VFX
        [SerializeField] private GameObject crackedGroundVFXPrefab = default!;
        [SerializeField] private GameObject explosionVFXPrefab = default!;

        // 여기서 상태를 갖지 않는 게 베스트이기 떄문에 Stateless 패턴으로 만들어봄
        // 상태를 갖는 게 아니라 고정된 값을 반환하는 메서드를 구현한다는 개념임
        // 참고) 필드로 $"{caster}" 를 구현하지 못한다고 나옴
        public string GetSkillControllerTag(UnitEntity caster) => $"{caster.name}_{skillName}_skillController";
        public string GetHitVFXTag(UnitEntity caster) => $"{caster.name}_{skillName}_hit";
        public string GetCastVFXTag(UnitEntity caster) => $"{caster.name}_{skillName}_cast";
        public string GetFallingSunVFXTag(UnitEntity caster) => $"{caster.name}_{skillName}_fallingSun";
        public string GetSkillRangeVFXTag(UnitEntity caster) => $"{caster.name}_{skillName}_skillRange";
        public string GetCrackedGroundVFXTag(UnitEntity caster) => $"{caster.name}_{skillName}_crackedGround";
        public string GetExplosionVFXTag(UnitEntity caster) => $"{caster.name}_{skillName}_explosion";

        public override void Activate(EnemyBoss caster, UnitEntity target)
        {
            IEnumerator sequence = ActivateSequence(caster, target);
            caster.ExecuteSkillSequence(sequence);
        }


        // 스킬 실행 시의 동작. 실행 주체는 caster임에 유의!
        public IEnumerator ActivateSequence(EnemyBoss caster, UnitEntity target)
        {
            // 범위 설정 - 그리드 위치를 계산, 다를 때에만 범위를 새롭게 계산한다.
            Vector2Int centerPos = MapManager.Instance!.ConvertToGridPosition(target.transform.position);
            if (caster.LastSkillCenter != centerPos)
            {
                HashSet<Vector2Int> skillRange = PositionCalculationSystem.CalculateRange(rangeOffset, centerPos, caster.transform.forward); // 방향은 크게 상관 없음
                caster.SetCurrentSkillRange(skillRange);
                caster.SetLastSkillCenter(centerPos);
            }

            // 범위를 기반으로 스킬 컨트롤러 생성, 스킬의 재생은 모두 여기서 담당함
            GameObject controllerObject = ObjectPoolManager.Instance.SpawnFromPool(GetSkillControllerTag(caster), target.transform.position, Quaternion.identity);
            BossExplosionSkillController? controller = controllerObject.GetComponent<BossExplosionSkillController>();

            if (controller != null)
            {
                // controllerObject.transform.SetParent(caster.gameObject.transform);

                // 1. Caster 위치에 Cast 이펙트를 실행함
                GameObject castVFXObject = ObjectPoolManager.Instance.SpawnFromPool(GetCastVFXTag(caster), caster.transform.position, Quaternion.identity);
                // castVFXObject.transform.SetParent(caster.gameObject.transform);
                SelfReturnVFXController castVFX = castVFXObject.GetComponent<SelfReturnVFXController>();
                if (castVFX != null)
                {
                    Debug.Log("[BossExplosionSkill]스킬 시전 이펙트 시작");
                    castVFX.Initialize(castTime);
                }
                caster.SetIsWaiting(true);

                // 시전 동작 중에는 대기 - 이거 기다리는 중에 비활성화되면 그 아래는 실행되지 않음
                yield return new WaitForSeconds(castTime);

                caster.SetIsWaiting(false);
                ObjectPoolManager.Instance.ReturnToPool(GetCastVFXTag(caster), castVFXObject);
                
                // 2. 스킬 시작
                controller.Initialize(caster, caster.GetCurrentSkillRange(), target);
            }
        }

        public override void InitializeSkillObjectPool(UnitEntity caster)
        {
            base.InitializeSkillObjectPool(caster);


            // 얘는 사실 null이면 안됨
            if (skillControllerPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(GetSkillControllerTag(caster), skillControllerPrefab, 1);
            }

            if (hitVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(GetHitVFXTag(caster), hitVFXPrefab, 10);
            }

            if (castVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(GetCastVFXTag(caster), castVFXPrefab, 1);
            }

            if (skillRangeVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(GetSkillRangeVFXTag(caster), skillRangeVFXPrefab, rangeOffset.Count);
            }

            if (crackedGroundVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(GetCrackedGroundVFXTag(caster), crackedGroundVFXPrefab, rangeOffset.Count);
            }

            if (fallingSunVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(GetFallingSunVFXTag(caster), fallingSunVFXPrefab, 1);
            }

            if (explosionVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(GetExplosionVFXTag(caster), explosionVFXPrefab, 1);
            }
        }

        // --- Public 게터(Getter) 프로퍼티 ---

        // Skill Configs
        public float CastTime => castTime;
        public float FallDuration => fallDuration;
        public float LingeringDuration => lingeringDuration;
        public float TickInterval => tickInterval;
        public float TickDamageRatio => tickDamageRatio;
        public float ExplosionDamageRatio => explosionDamageRatio;

        // Actual Effect
        public List<Vector2Int> RangeOffset => rangeOffset;
        public float DamageMultiplier => damageMultiplier;
        public float DotDamageMultiplier => dotDamageMultiplier;

        // SkillController Prefab
        public GameObject SkillControllerPrefab => skillControllerPrefab;

        // VFX Prefab
        public GameObject CastVFXPrefab => castVFXPrefab;
        public GameObject HitVFXPrefab => hitVFXPrefab;
    }
}