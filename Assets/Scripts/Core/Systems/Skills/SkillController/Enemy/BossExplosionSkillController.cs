using System.Collections.Generic;
using System.Collections;
using Skills.Base;
using UnityEngine;

// 보스 스킬에서 떨어지는 태양 VFX의 움직임을 제어하는 컴포넌트.
public class BossExplosionSkillController : FieldEffectController
{
    [Header("References")]
    [SerializeField] private BossExplosionSkill skillData;

    protected EnemyBoss _caster;
    private float casterAttackPower; // 스킬 시전 시점의 캐스터 공격력
    private UnitEntity mainTarget;
    private Vector3 centerPosition;

    private void Awake()
    {
        if (skillData == null)
        {
            Logger.LogError("[BossExplosionSkillController] 스킬 데이터가 할당되지 않음!");
            return;
        }
    }

    public void Initialize(EnemyBoss caster, IReadOnlyCollection<Vector2Int> skillRangeGridPositions, Vector3 centerPosition)
    {
        _caster = caster;
        casterAttackPower = caster.AttackPower;
        this.skillRangeGridPositions = skillRangeGridPositions;
        this.centerPosition = centerPosition;

        StopAllCoroutines();
        StartCoroutine(PlaySkillCoroutine());
    }

    private IEnumerator PlaySkillCoroutine()
    {
        // 1. 영역 표시
        VisualizeSkillRange();

        // 2. 해 파티클 목표 위치에 떨어지는 효과 실행
        GameObject sunParticleObj = ObjectPoolManager.Instance.SpawnFromPool(skillData.FallingSunVFXTag, centerPosition, Quaternion.identity);
        FallingSunVFXController sunParticleSystem = sunParticleObj.GetComponent<FallingSunVFXController>();
        if (sunParticleSystem != null)
        {
            sunParticleSystem.Initialize(_caster, skillData, skillData.FallingSunVFXDuration);
        }

        yield return new WaitForSeconds(skillData.FallDuration); // 낙하시간 동안 대기

        // 3. 낙하 후에 폭발 이펙트 실행, 대미지를 가함
        PlayExplosionVFX();
        ApplyInitialEffect(null); // 실제 효과

        // 4. 폭발 시 범위 타일들에 임팩트 대미지를 주고 지속 대미지를 입히는 필드가 남음
        PlayPeriodicVFX();
        StartCoroutine(PeriodicEffectCoroutine());

        yield return new WaitForSeconds(skillData.LingeringDuration); // 필드 지속시간 동안 대기


        Logger.Log($"[BossExplosionSkillController]오브젝트가 풀로 돌아감");
        ObjectPoolManager.Instance.ReturnToPool(skillData.SkillControllerTag, gameObject); // 풀로 되돌림
    }

    private void VisualizeSkillRange()
    {
        if (skillData.SkillRangeVFXTag == string.Empty)
        {
            Logger.LogError("[BossExplosionSkillController]스킬 범위 프리팹이 할당되지 않은 듯");
            return;
        }

        foreach (Vector2Int pos in skillRangeGridPositions)
        {
            if (MapManager.Instance!.CurrentMap!.IsValidGridPosition(pos.x, pos.y))
            {
                Vector3 worldPos = MapManager.Instance!.ConvertToWorldPosition(pos);

                // 오브젝트 풀에서 VFX 객체를 가져옴
                GameObject? vfxObj = ObjectPoolManager.Instance?.SpawnFromPool(skillData.SkillRangeVFXTag, worldPos, Quaternion.identity);

                if (vfxObj != null)
                {
                    // caster가 죽어도 이펙트는 사라지지 않음
                    // vfxObj.transform.SetParent(caster.transform);

                    var controller = vfxObj.GetComponent<SkillRangeVFXController>();
                    if (controller != null)
                    {
                        // 실행 주기는 컨트롤러 자체에서 관리됨
                        // 떨어지는 동안만 범위 표시 이펙트를 보여줌
                        controller.Initialize(pos, skillRangeGridPositions, skillData.FallDuration); 
                    }
                }
            }
        } 
    }

    protected override void ApplyInitialEffect(UnitEntity target)
    {
        AttackSource attackSource = new AttackSource(
            attacker: _caster,
            position: _caster.transform.position, // 크게 상관 없어서 이렇게 구현
            damage: casterAttackPower * skillData.ExplosionDamageRatio, // 폭발 대미지를 곱함
            type: AttackType.Magical,
            isProjectile: false,
            hitEffectTag: skillData.HitVFXTag,
            showDamagePopup: true
        );

        foreach (Vector2Int gridPos in skillRangeGridPositions)
        {
            Tile tile = MapManager.Instance.GetTile(gridPos.x, gridPos.y);
            if (tile?.OccupyingDeployable is Operator op)
            {
                op.TakeDamage(attackSource);
            }
        }
    }

    // 틱 대미지를 어떻게 넣을지는 나중에 생각합시다
    protected override void ApplyPeriodicEffect()
    {
        AttackSource attackSource = new AttackSource(
            attacker: _caster,
            position: _caster.transform.position, // 크게 상관 없어서 이렇게 구현
            damage: casterAttackPower * skillData.TickDamageRatio, // 폭발 대미지를 곱함
            type: AttackType.Magical,
            isProjectile: false,
            hitEffectTag: skillData.HitVFXTag,
            showDamagePopup: false
        );

        foreach (Vector2Int gridPos in skillRangeGridPositions)
        {
            Tile tile = MapManager.Instance.GetTile(gridPos.x, gridPos.y);
            if (tile?.OccupyingDeployable is Operator op)
            {
                op.TakeDamage(attackSource);
            }
        }
    }

    private IEnumerator PeriodicEffectCoroutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < skillData.LingeringDuration)
        {
            // 폭발 이후에 바로 대미지가 들어가지 않게 함
            yield return new WaitForSeconds(skillData.TickInterval);
            ApplyPeriodicEffect();
            elapsedTime += skillData.TickInterval;
        }
    }

    private void PlayExplosionVFX()
    {
        GameObject explosionObject = ObjectPoolManager.Instance.SpawnFromPool(skillData.ExplosionVFXTag, centerPosition, Quaternion.identity);

        ParticleSystem explosionVFX = explosionObject.GetComponent<ParticleSystem>();
        if (explosionVFX != null)
        {
            explosionVFX.Play(true);
        } 
    }
    
    private void PlayPeriodicVFX()
    {
        if (skillData.SkillRangeVFXTag == string.Empty)
        {
            Logger.LogError("[BossExplosionSkillController]바닥 파티클 시스템 프리팹이 할당되지 않은 듯");
            return;       
        }

        foreach (Vector2Int pos in skillRangeGridPositions)
        {
            if (MapManager.Instance!.CurrentMap!.IsValidGridPosition(pos.x, pos.y))
            {
                Vector3 worldPos = MapManager.Instance!.ConvertToWorldPosition(pos);

                // 오브젝트 풀에서 VFX 객체를 가져옴
                GameObject? vfxObj = ObjectPoolManager.Instance?.SpawnFromPool(skillData.CrackedGroundVFXTag, worldPos, Quaternion.identity);

                if (vfxObj != null)
                {
                    SkillRangeVFXController groundVFX = vfxObj.GetComponent<SkillRangeVFXController>();
                    if (groundVFX != null)
                    {
                        groundVFX.Initialize(pos, skillRangeGridPositions, skillData.LingeringVFXDuration);
                    }
                }
            }
        } 
    }
    // 사용하지 않는 필드는 명시적으로 예외를 설정함
    protected override void CheckTargetsInField()
    {
        throw new System.NotImplementedException();
    }
}
