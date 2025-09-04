using System.Collections.Generic;
using System.Collections;
using Skills.Base;
using UnityEngine;

// 보스 스킬에서 떨어지는 태양 VFX의 움직임을 제어하는 컴포넌트.
public class BossExplosionSkillController : FieldEffectController
{
    [Header("References")]
    [SerializeField] private BossExplosionSkill skillData;

    private UnitEntity mainTarget;
    private float castTime; // 보스가 스킬을 시전하는 시간
    private float fallDuration; // 해 파티클 낙하 시간
    private float lingeringDuration; // 폭발 후 틱 대미지 효과 지속 시간 

    private void Awake()
    {
        if (skillData == null)
        {
            Debug.LogError("[BossExplosionSkillController] 스킬 데이터가 할당되지 않음!");
            return;
        }
    }

    private void Start()
    {
        StartCoroutine(PlaySkillCoroutine());
    }

    public void Initialize(UnitEntity caster, IReadOnlyCollection<Vector2Int> skillRangeGridPositions, UnitEntity target)
    {
        this.caster = caster;
        this.skillRangeGridPositions = skillRangeGridPositions;
        mainTarget = target;

        StartCoroutine(PlaySkillCoroutine());
    }

    private IEnumerator PlaySkillCoroutine()
    {
        // 1. 영역 표시
        VisualizeSkillRange(caster, skillRangeGridPositions);

        // 2. 해 파티클 목표 위치에 떨어지는 효과 실행
        GameObject sunParticleObj = ObjectPoolManager.Instance.SpawnFromPool(skillData.GetFallingSunVFXTag(caster), mainTarget.transform.position, Quaternion.identity);
        FallingSunVFXController sunParticleSystem = sunParticleObj.GetComponent<FallingSunVFXController>();
        if (sunParticleSystem != null)
        {
            sunParticleSystem.Initialize();
        }
        yield return new WaitForSeconds(fallDuration); // 낙하시간 동안 대기

        // 3. 낙하 후에 폭발 이펙트 실행, 대미지를 가함

        // 필요) 폭발 이펙트 실행 메서드
        ApplyExplosionDamage(); // 실제 효과

        // 4. 폭발 시 범위 타일들에 임팩트 대미지를 주고 지속 대미지를 입히는 필드가 남음
        // 필요) 타일 틱뎀 이펙트 실행 메서드
        ApplyTickDamage(); // 틱댐 실제 효과

        yield return new WaitForSeconds(lingeringDuration); // 필드 지속시간 동안 대기
    }

    private void VisualizeSkillRange(UnitEntity caster, IReadOnlyCollection<Vector2Int> range)
    {
        if (skillData.GetSkillRangeVFXTag(caster) == string.Empty)
        {
            Debug.LogError("[BossExplosionSkillController]스킬 범위 프리팹이 할당되지 않은 듯");
            return;       
        }

        foreach (Vector2Int pos in range)
        {
            if (MapManager.Instance!.CurrentMap!.IsValidGridPosition(pos.x, pos.y))
            {
                Vector3 worldPos = MapManager.Instance!.ConvertToWorldPosition(pos);

                // 오브젝트 풀에서 VFX 객체를 가져옴
                GameObject? vfxObj = ObjectPoolManager.Instance?.SpawnFromPool(skillData.GetSkillRangeVFXTag(caster), worldPos, Quaternion.identity);

                if (vfxObj != null)
                {
                    vfxObj.transform.SetParent(caster.transform);

                    var controller = vfxObj.GetComponent<SkillRangeVFXController>();
                    if (controller != null)
                    {
                        // 실행 주기는 컨트롤러 자체에서 관리됨
                        controller.Initialize(pos, range, fallDuration); // 범위를 보여주는 시간은 Sun 파티클이 떨어지는 동안만
                    }
                }
            }
        } 
    }

    private void ApplyExplosionDamage()
    {
        AttackSource attackSource = new AttackSource(
            attacker: caster,
            position: caster.transform.position, // 크게 상관 없어서 이렇게 구현
            damage: caster.AttackPower * skillData.ExplosionDamageRatio, // 폭발 대미지를 곱함
            type: AttackType.Magical,
            isProjectile: false,
            hitEffectPrefab: skillData.HitVFXPrefab,
            hitEffectTag: skillData.GetHitVFXTag(caster)
        );

        foreach (Vector2Int gridPos in skillRangeGridPositions)
        {
            Tile tile = MapManager.Instance.GetTile(gridPos.x, gridPos.y);
            if (tile.OccupyingDeployable is Operator op)
            {
                op.TakeDamage(attackSource, true);
            }
        }
    }

    // 틱 대미지를 어떻게 넣을지는 나중에 생각합시다
    private void ApplyTickDamage()
    {
        AttackSource attackSource = new AttackSource(
            attacker: caster,
            position: caster.transform.position, // 크게 상관 없어서 이렇게 구현
            damage: caster.AttackPower * skillData.ExplosionDamageRatio, // 폭발 대미지를 곱함
            type: AttackType.Magical,
            isProjectile: false,
            hitEffectPrefab: skillData.HitVFXPrefab,
            hitEffectTag: skillData.GetHitVFXTag(caster)
        );

        foreach (Vector2Int gridPos in skillRangeGridPositions)
        {
            Tile tile = MapManager.Instance.GetTile(gridPos.x, gridPos.y);
            if (tile.OccupyingDeployable is Operator op)
            {
                op.TakeDamage(attackSource, true);
            }
        }
    }

    protected override void ApplyInitialEffect(UnitEntity target)
    {
        throw new System.NotImplementedException();
    }

    protected override void ApplyPeriodicEffect()
    {
        throw new System.NotImplementedException();
    }

    protected override void CheckTargetsInField()
    {
        throw new System.NotImplementedException();
    }
}
