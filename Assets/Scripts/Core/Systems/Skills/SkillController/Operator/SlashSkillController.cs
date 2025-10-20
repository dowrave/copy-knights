using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Skills.Base;

public class SlashSkillController : MonoBehaviour
{
    private Operator attacker = default!; // 죽은 경우에 대한 별도 처리가 없어서 일단 이렇게 구현
    private float effectDuration;
    private float firstDamageMultiplier;
    private float secondDamageMultiplier;
    private GameObject hitEffectPrefab = default!;
    private string hitEffectTag = string.Empty;
    private string skillPoolTag = string.Empty; // 풀로 되돌릴 때 사용
    private OperatorSkill OperatorSkill;

    private float firstDelay = 0.3f;
    private float secondDelay = 0.1f;

    // 파티클 시스템 관련
    [SerializeField] private ParticleSystem mainEffect = default!;

    // 대미지 적용된 적 추적
    private HashSet<Enemy> damagedEnemies = new HashSet<Enemy>();

    // 공격 가능 타일 좌표
    private HashSet<Vector2Int> skillRange = new HashSet<Vector2Int>();

    public void Initialize(Operator op, float duration, HashSet<Vector2Int> attackableGridPositions, float firstDmg, float secondDmg, GameObject hitEffectPrefab, string hitEffectTag, string skillPoolTag, OperatorSkill OperatorSkill)
    {
        attacker = op;
        effectDuration = duration;
        this.skillRange = attackableGridPositions;
        firstDamageMultiplier = firstDmg;
        secondDamageMultiplier = secondDmg;
        this.hitEffectPrefab = hitEffectPrefab;
        this.hitEffectTag = hitEffectTag;
        this.skillPoolTag = skillPoolTag;
        this.OperatorSkill = OperatorSkill;

        mainEffect.Play(true);

        StartCoroutine(SkillSequenceCoroutine());
    }

    private IEnumerator SkillSequenceCoroutine()
    {
        // 1번째 타격
        ApplyDamageInRange(firstDamageMultiplier);
        yield return new WaitForSeconds(firstDelay);

        // 2번째 타격 : 3번에 걸쳐 들어감. 간격은 0.15초.
        ApplyDamageInRange(secondDamageMultiplier);
        yield return new WaitForSeconds(secondDelay);
        ApplyDamageInRange(secondDamageMultiplier);
        yield return new WaitForSeconds(secondDelay);
        ApplyDamageInRange(secondDamageMultiplier);

        // 스킬 판정 후에는 공격 바로 가능하도록 공격 불가 버프 해제
        attacker.RemoveBuffFromSourceSkill(OperatorSkill);

        // --- 스킬 종료 및 정리 ---
        // 남은 vfxDuration 만큼 기다렸다가 비활성화 (오브젝트 풀링)
        // 위에서 0.3 + 0.2 = 0.5초를 사용했으므로 남은 시간만 기다림
        float remainingTime = effectDuration - 0.6f;
        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        mainEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ObjectPoolManager.Instance.ReturnToPool(skillPoolTag, gameObject);
    }


    // 지정된 범위 내의 모든 적에게 데미지를 적용하는 헬퍼 함수
    private void ApplyDamageInRange(float damageMultiplier)
    {
        // 1. 범위 내의 모든 적을 찾습니다 (중복 제거를 위해 HashSet 사용).
        HashSet<Enemy> enemiesInRange = new HashSet<Enemy>();
        foreach (Vector2Int gridPos in skillRange)
        {
            Tile tile = MapManager.Instance.GetTile(gridPos.x, gridPos.y);
            if (tile != null)
            {
                // 타일 위의 모든 적을 순회하며 enemiesInRange에 추가
                foreach (Enemy enemy in tile.EnemiesOnTile)
                {
                    enemiesInRange.Add(enemy);
                }
            }
        }

        // 2. 찾은 적들에게 데미지를 적용합니다. - 나중에 수를 제한할 수도 있음
        foreach (Enemy enemy in enemiesInRange)
        {            
            if (enemy != null)
            {
                // 데미지 처리 로직...
                AttackSource attackSource = new AttackSource(
                    attacker: attacker,
                    position: attacker.transform.position,
                    damage: attacker.AttackPower * damageMultiplier,
                    type: attacker.AttackType,
                    isProjectile: false,
                    hitEffectTag: hitEffectTag,
                    showDamagePopup: false
                );

                enemy.TakeDamage(attackSource);

                // enemy.TakeDamage(...);

                // 피격 이펙트 생성 등...

                // 처리된 적으로 등록
                // alreadyHitEnemies.Add(enemy);
            }
        }
    }

    private void OnDestroy()
    {
        damagedEnemies.Clear();
    }
}
