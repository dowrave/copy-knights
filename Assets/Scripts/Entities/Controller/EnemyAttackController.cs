using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Enemy의 공격 로직을 처리함
// 저지당했을 경우도 여기서 처리
public class EnemyAttackController: IEnemyAttackReadOnly
{
    private Enemy _owner;

    public float AttackCooldown { get; protected set; } // 공격 쿨다운
    public float AttackDuration { get; protected set; } // 공격 모션 시간
    public AttackType CurrentAttackType { get; protected set; }

    private Barricade _targetBarricade;
    private Operator _blockingOperator; // 자신을 저지 중인 오퍼레이터
    private UnitEntity? _currentTarget;
    private List<UnitEntity> _targetsInRange = new List<UnitEntity>();
    private bool _stopAttacking = false; // 인위적으로 넣은 공격 가능 / 불가능 상태

    public Barricade TargetBarricade => _targetBarricade; 
    public Operator BlockingOperator => _blockingOperator;
    public IReadOnlyList<UnitEntity> TargetsInRange => _targetsInRange;
    public bool StopAttacking => _stopAttacking;
    public UnitEntity? CurrentTarget
    {
        get => _currentTarget;
        protected set
        {
            // 타겟이 기존 값과 동일하다면 세터 실행 X
            if (_currentTarget == value) return;

            // 기존 타겟의 이벤트 구독 해제
            if (_currentTarget != null)
            {
                _currentTarget.RemoveAttackingEntity(_owner);
            }

            _currentTarget = value;

            if (_currentTarget != null)
            {
                NotifyTarget();
            }
        }
    } // 공격 대상

    public EnemyAttackController(Enemy owner)
    {
        _owner = owner;
    }

    public void Initialize()
    {
        CurrentAttackType = _owner.BaseData.AttackType;
    }

    // 행동 제약에 관계없이 업데이트되어야 하는 요소들
    public void UpdateAllCooldowns()
    {
        UpdateAttackTimes();
    }

    // 공격을 했으면 true, 하지 않았으면 false 반환
    public bool OnUpdate()
    {
        // 공격 범위 내의 적 리스트 & 현재 공격 대상 갱신
        SetCurrentTarget();

        // 저지당함 - 근거리 공격
        if (_blockingOperator != null)
        {
            if (CanAttack())
            {
                PerformMeleeAttack(CurrentTarget!, _owner.AttackPower);
            }

            // 저지 당한 상태라면 공격 여부에 관계 없이 추가 동작을 막기 위해 true 반환
            // 저지 관련 로직이 복잡해진다면 저지 부분은 별도의 컴포넌트로 분리 ㄱㄱ
            return true; 
        }
        // 저지당하지 않음
        else
        {
            // 바리케이트가 타겟일 경우
            if (_targetBarricade != null && Vector3.Distance(_owner.transform.position, _targetBarricade.transform.position) < 0.5f)
            {
                PerformMeleeAttack(_targetBarricade, _owner.AttackPower);
                return true;
            }

            // 타겟이 있고, 공격이 가능한 상태
            if (CanAttack())
            {
                Attack(CurrentTarget!, _owner.AttackPower);
                return true;
            }
        }
        
        // 이동할 수 없는 상황을 모두 처리했음
        // 나머지 상황은 이동 가능
        return false;
    }

    public void UpdateAttackTimes()
    {
        // 공격 모션 지속 시간
        if (AttackDuration > 0f)
        {
            AttackDuration -= Time.deltaTime;
        }
        
        // 쿨다운 = 다음 공격 가능 시간
        if (AttackCooldown > 0f)
        {
            AttackCooldown -= Time.deltaTime;
        }
    }

    public void SetCurrentTarget()
    {
        // 현재 타겟이 나를 저지 중인 오퍼레이터라면 변경할 필요 X
        if (_blockingOperator != null && CurrentTarget == _blockingOperator) return;

        // 현재 타겟이 비활성화라면 null 처리
        if (!CheckCurrentTargetValidation()) CurrentTarget = null;

        // 1. 자신을 저지하는 오퍼레이터를 타겟으로 설정
        if (_blockingOperator != null)
        {
            CurrentTarget = _blockingOperator;
            return;
        }

        // 2. 공격 범위 내의 타겟
        if (_targetsInRange.Count > 0)
        {
            // 타겟 선정
            UnitEntity? newTarget = _targetsInRange
                .OfType<Operator>() // 오퍼레이터
                .OrderByDescending(o => o.DeploymentOrder) // 가장 나중에 배치된 오퍼레이터 
                .FirstOrDefault();

            // 타겟의 유효성 검사 : 공격 범위 내에 있고 null이 아닐 때
            if (newTarget != null || _targetsInRange.Contains(newTarget))
            {
                CurrentTarget = newTarget;
            }

            return;
        }

        // 3. 위의 두 조건에 해당하지 않는다면 타겟을 제거한다.
        CurrentTarget = null;
    }

    public bool CanAttack()
    {
        return CheckCurrentTargetValidation() &&
            AttackCooldown <= 0 &&
            AttackDuration <= 0 &&
            !_stopAttacking; 
    }

    private bool CheckCurrentTargetValidation() => CurrentTarget != null && CurrentTarget.gameObject.activeInHierarchy;
    

    // CurrentTarget에게 자신이 공격하고 있음을 알림
    public void NotifyTarget() => CurrentTarget?.AddAttackingEntity(_owner);


    public void Attack(UnitEntity target, float damage)
    {
        float polishedDamage = Mathf.Floor(damage);
        PerformAttack(target, polishedDamage);
    }

    protected void PerformAttack(UnitEntity target, float damage)
    {
        AttackType finalAttackType = _owner.AttackType;
        bool showDamagePopup = false;

        foreach (var buff in _owner.ActiveBuffs)
        {
            buff.OnBeforeAttack(_owner, ref damage, ref finalAttackType, ref showDamagePopup);
        }

        switch (_owner.AttackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target, damage);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target, damage);
                break;
        }
        
        foreach (var buff in _owner.ActiveBuffs)
        {
            buff.OnAfterAttack(_owner, target);
        }
    }

    protected void PerformMeleeAttack(UnitEntity target, float damage)
    {
        SetAttackTimings(); // 이걸 따로 호출하는 경우가 있어서 여기서 다시 설정

        AttackSource attackSource = new AttackSource(
            attacker: _owner,
            position: _owner.transform.position,
            damage: damage,
            type: _owner.AttackType,
            isProjectile: false,
            hitEffectTag: _owner.BaseData.HitVFXTag,
            showDamagePopup: false
        );

        PlayMeleeAttackEffect(target, attackSource);
        target.TakeDamage(attackSource);
    }

    protected void PerformRangedAttack(UnitEntity target, float damage)
    {
        SetAttackTimings();
        
        if (_owner.BaseData.ProjectilePrefab != null)
        {
            // 투사체 생성 위치
            Vector3 spawnPosition = _owner.transform.position;
            GameObject? projectileObj = ObjectPoolManager.Instance!.SpawnFromPool(_owner.BaseData.ProjectileTag, spawnPosition, Quaternion.identity);

            if (projectileObj != null)
            {
                Projectile? projectile = projectileObj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(_owner, target, damage, false, _owner.BaseData.ProjectileTag, _owner.BaseData.HitVFXTag, _owner.AttackType);
                }
            }
        }
    }

    #region Attack Range Collider

    public void OnTargetEnteredAttackRange(DeployableUnitEntity deployable) 
    {
        if (CanBeAddedInAttackRange(deployable))
        {
            _targetsInRange.Add(deployable);
        }
    }

    public bool CanBeAddedInAttackRange(DeployableUnitEntity deployable)
    {
        // 현재 배치된 상태이면서 공격 대상 후보에 없는 Ally(유저 진영)일 때 true를 반환
        return deployable.Faction == Faction.Ally && 
                deployable.IsDeployed && 
                !_targetsInRange.Contains(deployable); 
    }

    public void OnTargetExitedAttackRange(DeployableUnitEntity deployable)
    {
        if (_targetsInRange.Contains(deployable))
        {
            _targetsInRange.Remove(deployable);
        }
    }

    #endregion



    // 공격 모션 시간, 공격 쿨타임 시간 설정
    public void SetAttackTimings()
    {
        if (AttackDuration <= 0f)
        {
            SetAttackDuration();
        }
        if (AttackCooldown <= 0f)
        {
            SetAttackCooldown();
        }
    }

    public void SetAttackDuration(float? intentionalDuration = null)
    {
        AttackDuration = _owner.AttackSpeed / 3f;
    }

    public void SetAttackCooldown(float? intentionalCooldown = null)
    {
        AttackCooldown = _owner.AttackSpeed;
    }

    public void HandleDeployableDied(DeployableUnitEntity disabledEntity)
    {
        if (_targetsInRange.Contains(disabledEntity))
        {
            _targetsInRange.Remove(disabledEntity);
        }

        if (_blockingOperator == disabledEntity)
        {
            _blockingOperator = null;
        }

        // 타겟이 범위에서 벗어났어도 현재 타겟으로 지정되는 상황이 있을 수 있으니 위 조건과는 별도로 구현했음
        if (CurrentTarget == disabledEntity)
        {
            CurrentTarget = null;
        }
    }

    public void UpdateBlockingOperator(Operator op)
    {
        _blockingOperator = op;
    }

    protected void PlayMeleeAttackEffect(UnitEntity target, AttackSource attackSource)
    {
        // 이펙트 처리
        if (_owner.MeleeAttackEffectTag != null && _owner.BaseData.MeleeAttackEffectPrefab != null)
        {
            GameObject? effectObj = ObjectPoolManager.Instance!.SpawnFromPool(
                   _owner.MeleeAttackEffectTag,
                   _owner.transform.position,
                   Quaternion.identity
            );

            if (effectObj != null)
            {
                CombatVFXController? combatVFXController = effectObj.GetComponent<CombatVFXController>();
                if (combatVFXController != null)
                {
                    combatVFXController.Initialize(attackSource, target, _owner.MeleeAttackEffectTag);
                }
            }
        }
    }
    

    public void SetStopAttacking(bool isAttacking)
    {
        _stopAttacking = isAttacking;
    }

    public void SetCurrentBarricade(Barricade? barricade)
    {
        _targetBarricade = barricade;
    } 

    protected bool IsTargetInRange(UnitEntity target)
    {
        return Vector3.Distance(target.transform.position, _owner.transform.position) <= _owner.AttackRange;
    }
}