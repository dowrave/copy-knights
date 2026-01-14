using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class OpAttackController: OpActionController
{
    // 공격 범위 내의 적들
    private List<Enemy> _enemiesInRange = new List<Enemy>();
    public IReadOnlyList<Enemy> EnemiesInRange => _enemiesInRange;

    public OpAttackController() { }

    public override void OnDeploy()
    {
        base.OnDeploy();
        RegisterTiles();
    }

    // Tile들에게 자신을 공격 범위로 삼고 있는 Operator임을 알림
    public void RegisterTiles()
    {
        foreach (Vector2Int eachPos in _currentActionableGridPos)
        {
            Tile? targetTile = MapManager.Instance!.GetTile(eachPos.x, eachPos.y);
            if (targetTile != null)
            {
                targetTile.RegisterOperator(_owner);

                // 타일 등록 시점에 그 타일에 있는 적의 정보도 Operator에게 전달함
                foreach (Enemy enemy in targetTile.EnemiesOnTile)
                {
                    OnEnemyEnteredRange(enemy);
                }
            }
        }
    }

    protected override void SetCurrentTarget()
    {
        // 현재의 target이 비활성화됐다면 null로 전환함. 조건문의 null 체크는 필수
        if (CurrentTarget != null && !CurrentTarget.gameObject.activeInHierarchy) CurrentTarget = null;

        // 1. 저지 중일 때 -> 저지 중인 적을 우선 순위로 공격
        IReadOnlyList<Enemy> blockedEnemies = _owner.BlockedEnemies;

        if (blockedEnemies.Count > 0)
        {
            for (int i = 0; i < blockedEnemies.Count; i++)
            {
                if (blockedEnemies[i])
                {
                    CurrentTarget = blockedEnemies[i];
                    break;
                }
            }
            return;
        }

        // 2. 저지 중이 아닐 때에는 공격 범위 내의 적 중에서 공격함
        if (_enemiesInRange.Count > 0)
        {
            CurrentTarget = _enemiesInRange
                .Where(e => e != null && e.gameObject != null) // 파괴 검사 & null 검사 함께 수행
                .OrderBy(E => E.Navigator.GetRemainingPathDistance(E.CurrentPathIndex)) // 살아있는 객체 중 남은 거리가 짧은 순서로 정렬
                .FirstOrDefault(); // 가장 짧은 거리의 객체를 가져옴
            
            return;
        }

        // 저지 중인 적도 없고, 공격 범위 내의 적도 없다면 현재 타겟은 없음
        CurrentTarget = null;
        
    }

    protected override void ValidateCurrentTarget()
    {
        if (CurrentTarget == null) return;
        if (_owner.BlockedEnemies.Contains(CurrentTarget)) return;

        // 범위에서 벗어난 경우
        if (!IsCurrentTargetInRange())
        {
            CurrentTarget = null;
        }
    }

    private bool IsCurrentTargetInRange()
    {
        foreach (Vector2Int eachPos in _currentActionableGridPos)
        {
            Tile? eachTile = MapManager.Instance!.GetTile(eachPos.x, eachPos.y);
            if (eachTile != null && eachTile.EnemiesOnTile.Contains(CurrentTarget))
            {
                return true;
            }
        }
        return false; 
    }

    public override void PerformAction(UnitEntity target, float damage)
    {
        bool showDamagePopup = false;
        float polishedDamage = Mathf.Floor(damage);

        // 공격이 나가는 시점에 쿨타임이 돌게 수정
        // 실제 공격을 수행할 때 어떻게 수행되는지가 다르므로 PerformAttack 밖에서 구현한다.
        // 예시) DoubleShot의 경우 PerformAttack 안에서 구현하면 쿨타임이 있는데 스킬이 나가는 이상한 구현이 됨
        SetActionDuration();
        SetActionCooldown();

        PerformAttack(target, polishedDamage, showDamagePopup);
    }

    private void PerformAttack(UnitEntity target, float damage, bool showDamagePopup)
    {
        float spBeforeAttack = _owner.CurrentSP;
        AttackType finalAttackType = _owner.AttackType;

        // 스킬 시스템에서 버프로 변환 중
        // 공격에만 적용되는 버프 적용
        foreach (var buff in _owner.ActiveBuffs)
        {
            buff.OnBeforeAttack(_owner, ref damage, ref finalAttackType, ref showDamagePopup);
        }

        // 실제 공격 수행
        switch (_owner.OperatorData.AttackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target, damage, finalAttackType, showDamagePopup);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target, damage, finalAttackType, showDamagePopup);
                break;
        }

        // 공격 후 SP 회복 로직
        if (_owner.CurrentSkill.autoRecover && // 자동회복이 아니면서
            _owner.IsSkillOn &&
            spBeforeAttack != _owner.MaxSP) // 스킬이 실려서 나간 공격일 때는 SP 회복 X 
        {
            _owner.SetCurrentSP(_owner.CurrentSP + 1);
        }

        // 공격 후 동작
        foreach (var buff in _owner.ActiveBuffs.ToList()) // buff가 제거될 수 있기 때문에 복사본으로 안전하게 진행
        {
            buff.OnAfterAttack(_owner, target);
        }
    }

    private void PerformMeleeAttack(UnitEntity target, float damage, AttackType attackType, bool showDamagePopup = false)
    {
        AttackSource attackSource = new AttackSource(
            attacker: _owner,
            position: _owner.transform.position,
            damage: damage,
            type: attackType,
            isProjectile: false,
            hitEffectTag: _owner.OperatorData.HitVFXTag,
            showDamagePopup: showDamagePopup
        );

        _owner.PlayMeleeAttackEffect(target, attackSource);
        target.TakeDamage(attackSource);
    }

    private void PerformRangedAttack(UnitEntity target, float damage, AttackType attackType, bool showDamagePopup = false)
    {
        if (_owner.OperatorData.ProjectilePrefab != null)
        {
            // 투사체 생성 위치
            Vector3 spawnPosition = _owner.transform.position + _owner.transform.forward * 0.25f;

            GameObject? projectileObj = ObjectPoolManager.Instance!.SpawnFromPool(_owner.OperatorData.ProjectileTag, spawnPosition, Quaternion.identity);
            if (projectileObj != null)
            {
                Projectile? projectile = projectileObj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    // _owner.PlayMuzzleVFX();
                    projectile.Initialize(_owner, target, damage, showDamagePopup, _owner.OperatorData.ProjectileTag, _owner.OperatorData.HitVFXTag, _owner.AttackType);
                }
            }
        }
    }

    public override void ResetStates()
    {
        _enemiesInRange.Clear();
        CurrentTarget = null;
    }

    public override void OnTargetDespawn(UnitEntity target)
    {
        if (target is Enemy enemy && enemy == CurrentTarget)
        {
            RemoveEnemyInRange(enemy);            
            CurrentTarget = null;
        }
    }

    // 외부 노출용 세터 메서드
    public void ResetCurrentTarget()
    {
        CurrentTarget = null;
    }

    public void UnregisterTiles()
    {
        foreach (Vector2Int eachPos in _currentActionableGridPos)
        {
            Tile? targetTile = MapManager.Instance!.GetTile(eachPos.x, eachPos.y);
            if (targetTile != null)
            {
                targetTile.UnregisterOperator(_owner);
            }
        }
    }

    // 공격 범위 내에 있는 적을 제거함
    public void RemoveEnemyInRange(Enemy enemy)
    {
        if (_enemiesInRange.Contains(enemy))
        {
            _enemiesInRange.Remove(enemy);
        }
    }

    public override void OnEnemyEnteredRange(Enemy enemy)
    {
        if (!_enemiesInRange.Contains(enemy))
        {
            _enemiesInRange.Add(enemy);
        }   
    }

    public override void OnEnemyExitedRange(Enemy enemy)
    {
        // 여전히 공격 범위 내에 해당 적이 있는가를 검사
        foreach (var gridPos in _currentActionableGridPos)
        {
            Tile? targetTile = MapManager.Instance!.GetTile(gridPos.x, gridPos.y);
            if (targetTile != null && targetTile.EnemiesOnTile.Contains(enemy))
            {
                return;
            }
        }

        // 공격 범위에서 완전히 이탈한 경우에 제거
        _enemiesInRange.Remove(enemy);
        if (CurrentTarget == enemy)
        {
            CurrentTarget = null; // 현재 타겟이 나간 경우 null로 설정
        }
    }

    // 유니티로 돌아가는 로직이 아님에 주의
    public override void OnDisabled()
    {
        UnregisterTiles();
    }
}