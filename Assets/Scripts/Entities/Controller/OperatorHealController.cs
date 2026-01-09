using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class OperatorHealController: OperatorActionController
{
    private List<Operator> _targetsInRange = new List<Operator>();
    public IReadOnlyList<Operator> TargetsInRange => _targetsInRange;

    protected override void SetCurrentTarget()
    {
        GetTargetsInRange();

        if (_targetsInRange.Count > 0)
        {
            CurrentTarget = _targetsInRange
                .OrderBy(target => target.Health.CurrentHealth)
                .FirstOrDefault();
        }
        else
        {
            CurrentTarget = null; 
        }
    }

    // 사거리 내의 힐을 받을 대상들을 수집
    private void GetTargetsInRange()
    {
        _targetsInRange.Clear();

        Map? CurrentMap = MapManager.Instance!.CurrentMap;

        if (CurrentMap != null)
        {
            foreach (Vector2Int gridPos in CurrentActionableGridPos)
            {
                Tile? targetTile = CurrentMap.GetTile(gridPos.x, gridPos.y);
                if (targetTile != null)
                {
                    Operator? deployable = targetTile.OccupyingDeployable as Operator;

                    if (deployable != null && 
                        deployable.Faction == _owner.Faction && 
                        deployable.Health.CurrentHealth < deployable.Health.MaxHealth)
                    {
                        _targetsInRange.Add(deployable);
                    }
                }
            }
        }
    }

    public override void PerformAction(UnitEntity target, float value)
    {
        float polishedDamage = Mathf.Floor(value);
        Heal(target, polishedDamage);

        SetActionDuration(); // 공격 모션 시간 설정
        SetActionCooldown(); // 공격 쿨다운 설정
    }

    public override void ResetState()
    {
        _targetsInRange.Clear();
        CurrentTarget = null;
    }

    private void Heal(UnitEntity target,  float healValue)
    {
        // 여기서 Ranged 여부가 결정하는 건 투사체의 유무임
        // 즉 Melee여도 원거리를 가질 수 있음 -- 사정거리랑 공격 타입을 별도로 구현했기 때문에 이런 현상이 발생했다.
        if (_owner.OperatorData.AttackRangeType == AttackRangeType.Ranged)
        {
            PerformRangedAction(target, healValue, _owner.AttackType);
        }
        else
        {
            AttackSource attackSource = new AttackSource(
                attacker: _owner,
                position: _owner.transform.position,
                damage: healValue,
                type: _owner.AttackType,
                isProjectile: true,
                hitEffectTag: _owner.HitEffectTag,
                showDamagePopup: false
            );
            target.TakeHeal(attackSource);
        }
    }

    private void PerformRangedAction(UnitEntity target, float damage, AttackType attackType, bool showDamagePopup = false)
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
                    projectile.Initialize(_owner, target, damage, showDamagePopup, _owner.OperatorData.ProjectileTag, _owner.OperatorData.HitVFXTag, _owner.AttackType);
                }
            }
        }
    }

    // 오버라이드하되 구현 하지 않음(target이 계속 초기화되기 때문)
    public override void OnTargetDespawn(UnitEntity target) { }
}