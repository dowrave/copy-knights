using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;


public class Operator : DeployableUnitEntity, ICombatEntity, ISkill, IRotatable
{
    [SerializeField] // 필드 직렬화, Inspector에서 이 필드 숨기기
    private OperatorData data;
    private OperatorStats currentStats;

    // ICombatEntity 필드
    public AttackType AttackType => data.attackType;
    public AttackRangeType AttackRangeType => data.attackRangeType;
    public float AttackPower => currentStats.attackPower;
    public float AttackSpeed => currentStats.attackSpeed; 
    public float AttackCooldown { get; private set; }


    // IRotatble 필드
    [SerializeField, HideInInspector]
    public Vector3 facingDirection = Vector3.left;
    public Vector3 FacingDirection
    {
        get => facingDirection;
        private set
        {
            facingDirection = value.normalized;
            transform.forward = facingDirection;
            UpdateDirectionIndicator(facingDirection);
        }
    }

    // 저지 관련
    private List<Enemy> blockedEnemies; // 저지 중인 적들
    public IReadOnlyList<Enemy> BlockedEnemies => blockedEnemies.AsReadOnly();

    public int deploymentOrder { get; private set; } // 배치 순서
    private bool isDeployed = false; // 배치 완료 시 true
    private UnitEntity currentTarget;
    //private float attackCooldown = 0f; // data.baseStats에서 들어오는 AttackSpeed 값에 의해 결정됨


    public float CurrentSP 
    {
        get { return currentStats.currentSP; }
        //set {}
    }

    [SerializeField] private GameObject deployableBarUIPrefab;
    private DeployableBarUI deployableBarUI;

    // 공격 범위 내에 있는 적들 
    List<Enemy> enemiesInRange = new List<Enemy>();

    private SpriteRenderer directionIndicator;


    // 필드 끝 --------------------------------------------------------

    public override void Initialize(UnitData unitData)
    {
        base.Initialize(unitData); // OperatorData로 초기화됨
        InitializeOperatorProperties();
    }

    protected override void InitializeData(UnitData unitData)
    {
        if (unitData is OperatorData operatorData)
        {
            data = operatorData;
            currentStats = data.stats;
        }
        else
        {
            Debug.LogError("들어온 데이터가 deployableUnitData가 아님!");
        }
    }

    private void InitializeOperatorProperties()
    {
        CreateDirectionIndicator(); 
    }

    public void SetDirection(Vector3 direction)
    {
        FacingDirection = direction;
    }

    private void CreateOperatorBarUI()
    {
        if (deployableBarUIPrefab != null)
        {
            GameObject uiObject = Instantiate(deployableBarUIPrefab, transform);
            deployableBarUI = uiObject.GetComponentInChildren<DeployableBarUI>();
            deployableBarUI.Initialize(this);
        }
    }

    public void Update()
    {
        if (IsDeployed)
        {
            if (AttackCooldown > 0)
            {
                UpdateAttackCooldown();
            }
            RecoverSP();

            ValidateCurrentTarget();

            // 공격 대상이 없다면
            if (currentTarget == null)
            {
                FindTarget(); // currentTarget 업데이트 시도
            }

            if (CanAttack())
            {
                Attack(currentTarget);
            }

        }
    }

    // 적에게 공격 중인 오퍼레이터를 알림
    private void SetAndNotifyTarget(UnitEntity newTarget)
    {
        if (currentTarget != null)
        {
            newTarget.RemoveAttackingOperator(this);
        }

        if (currentTarget != null)
        {
            currentTarget = newTarget;
            newTarget.AddAttackingOperator(this);
        }
    }

    // currentTarget 설정 로직
    private void FindTarget()
    {
        // 1. 저지 중일 때에는 저지 중인 적 중에서 공격
        if (blockedEnemies.Count > 0)
        {
            currentTarget = blockedEnemies[0]; // 첫 번째 저지된 적을 타겟으로
            SetAndNotifyTarget(currentTarget);
            return;
        }

        GetEnemiesInAttackRange(); // 공격 범위 내의 적을 얻음

        // 2. 저지 중이 아닐 때에는 공격 범위 내의 적 중에서 공격함
        // enemiesInRange 업데이트
        if (enemiesInRange.Count > 0)
        {
            string enemiesInfo = string.Join(", ", enemiesInRange.Select((enemy, index) =>
                $"Enemy {index}: {enemy.name} (Health: {enemy.CurrentHealth}/{enemy.MaxHealth}, Position: {enemy.transform.position})"));

            currentTarget = enemiesInRange[0];
            SetAndNotifyTarget(currentTarget);
            return;
        }
    }

    public void Attack(UnitEntity target)
    {
        if (!IsAttackCooldownComplete || !(target is Enemy enemy)) return;

        base.Attack(target); // 공격 가능할 때, PerformAttack을 실행시키고 쿨다운도 시작됨

    }

    protected override void PerformAttack(UnitEntity target)
    {
        switch (data.attackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target);
                break;
        }
    }

    private void PerformMeleeAttack(Unit target)
    {
        target.TakeDamage(currentStats.attackPower);
    }

    private void PerformRangedAttack(Unit target)
    {
        if (data.projectilePrefab != null)
        {
            // 투사체 생성 위치
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
            
            GameObject projectileObj = Instantiate(data.projectilePrefab, spawnPosition, Quaternion.identity);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(target, currentStats.attackPower);
            }
        }
    }


    // 공격 타일에 있는 적들을 반환함
    private void GetEnemiesInAttackRange()
    {
        enemiesInRange.Clear();
        Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);

        foreach (Vector2Int offset in data.attackableTiles)
        {
            Vector2Int rotatedOffset = RotateOffset(offset, facingDirection);
            Vector2Int targetGridPos = operatorGridPos + rotatedOffset;

            Tile targetTile = MapManager.Instance.CurrentMap.GetTile(targetGridPos.x, targetGridPos.y);
            if (targetTile != null)
            {
                // 타일 위의 적들을 보는 로직은 Tile.cs에 구현됨
                List<Enemy> enemiesOnTile = targetTile.GetEnemiesOnTile();
                enemiesInRange.AddRange(enemiesOnTile);
            }
        }

        enemiesInRange = enemiesInRange.Distinct().ToList(); // 중복 제거해서 반환
    }


    // Operator 회전
    public Vector2Int RotateOffset(Vector2Int offset, Vector3 direction)
    {
        if (direction == Vector3.left) return offset;
        if (direction == Vector3.right) return new Vector2Int(-offset.x, -offset.y);
        if (direction == Vector3.forward) return new Vector2Int(-offset.y, offset.x); // 2차원(y평면)으로 보면 위쪽
        if (direction == Vector3.back) return new Vector2Int(offset.y, -offset.x); // 2차원 기준 아래쪽
        return offset;
    }
    public Vector2Int[] GetAttackableTiles()
    {
        return data.attackableTiles;
    }

    public bool CanAttack(Vector3 targetPosition)
    {
        Vector2Int relativePosition = WorldToRelativeGridPosition(targetPosition);
        return System.Array.Exists(data.attackableTiles, tile => tile == relativePosition);
    }

    private Vector2Int WorldToRelativeGridPosition(Vector3 worldPosition)
    {
        if (MapManager.Instance.CurrentMap != null)
        {
            Vector2Int absoluteGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(worldPosition);
            Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);
            return absoluteGridPos - operatorGridPos;
        }
        return Vector2Int.zero;
    }
    

    public void SetDeploymentOrder(int order)
    {
        // 이 order라는 값을 관리할 오브젝트가 또 따로 필요함
        // 나중에 StageManager를 구현하든지 해서 거기다가 때려넣자
        deploymentOrder = order;
    }

    // --- 저지 관련 메서드들

    // 이 오퍼레이터가 적을 저지할 수 있는 상태인가?
    public bool CanBlockEnemy()
    {
        return blockedEnemies.Count < currentStats.maxBlockableEnemies;
    }

    // 저지 가능하다면 현 저지수 + 1
    public bool TryBlockEnemy(Enemy enemy)
    {
        if (CanBlockEnemy())
        {
            blockedEnemies.Add(enemy);
            Debug.Log($"저지 시작: {enemy}");
            return true;
        }
        return false;
    }

    public void UnblockEnemy(Enemy enemy)
    {
        Debug.LogWarning("적 저지 해제");
        blockedEnemies.Remove(enemy);
    }

    public void UnblockAllEnemies()
    {
        blockedEnemies.Clear();
    }

    // SP 로직 추가
    private void RecoverSP()
    {
        if (IsDeployed == false) { return;  }

        float oldSP = currentSP;
        if (data.autoRecoverSP)
        {
            currentSP = Mathf.Min(currentSP + data.SpRecoveryRate * Time.deltaTime, data.maxSP);    

        }

        if (currentSP != oldSP)
        {
            deployableBarUI.UpdateSPBar(currentSP, data.maxSP);
            //operatorUI.UpdateOperatorUI(this);
        }
    }

    public bool TryUseSkill(float spCost)
    {
        if (currentSP >= spCost)
        {
            currentSP -= spCost;

            return true;
        }
        return false; 
    }

    public override void TakeDamage(AttackType attackType, float damage)
    {
        base.TakeDamage(attackType, damage);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    protected override void Die()
    {
        // 사망 후 작동해야 하는 로직이 있을 듯?
        UnblockAllEnemies();

        // 오브젝트 파괴
        Destroy(deployableBarUI.gameObject); // 하단 체력/SP 바
        Destroy(directionIndicator.gameObject); // 방향 표시기
        base.Die();

        // 하단 UI 활성화
        DeployableManager.Instance.OnDeployableRemoved(this);
    }

    public void UseSkill()
    {
        // 스킬 사용 로직
        Debug.Log("스킬 버튼 클릭됨");
    }


    public override void OnClick()
    {
        base.OnClick();
        HighlightAttackRange();
    }

    // 공격 대상인 적이 죽었을 때 작동함. 저지 해제와 별개로 구현
    public void OnTargetLost(Enemy enemy)
    {

        // 공격 대상에서 제거
        if (currentTarget == enemy)
        {
            currentTarget = null;
        }

        // 범위 내 적 리스트에서 제거
        enemiesInRange.Remove(enemy); // 안하면 리스트에 파괴된 오브젝트가 남아서 0번 인덱스를 캐치하지 못함
    }

    /// <summary>
    /// 방향 표시 UI 생성
    /// </summary>
    private void CreateDirectionIndicator()
    {
        GameObject indicator = new GameObject("DirectionIndicator");
        indicator.transform.SetParent(transform);
        indicator.transform.localPosition = new Vector3(0, -0.1f, 0);
        indicator.transform.localRotation = Quaternion.Euler(90, 0, -90);
        indicator.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        directionIndicator = indicator.AddComponent<SpriteRenderer>();
        directionIndicator.sprite = Resources.Load<Sprite>("direction_sprite");
        directionIndicator.enabled = false;
    }

    public void UpdateDirectionIndicator(Vector3 direction)
    {
        if (directionIndicator != null)
        {
            float angle = Vector3.SignedAngle(Vector3.left, direction, Vector3.up);

            // x축 회전 : 바닥에 눕히기 / z축 중심으로 -angle만큼 회전시키면 방향이 맞음(테스트 완료)
            directionIndicator.transform.localRotation = Quaternion.Euler(90, 0, -90);
        }
    }

    public void ShowDirectionIndicator(bool show)
    {
        if (directionIndicator != null)
        {
            directionIndicator.enabled = show;
        }
    }

    public void HighlightAttackRange()
    {
        if (MapManager.Instance.CurrentMap == null) return;

        Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);
        List<Tile> tilesToHighlight = new List<Tile>();

        foreach (Vector2Int offset in data.attackableTiles)
        {
            Vector2Int rotatedIOffset = RotateOffset(offset, facingDirection);
            Vector2Int targetGridPos = operatorGridPos + rotatedIOffset;
            Tile targetTile = MapManager.Instance.CurrentMap.GetTile(targetGridPos.x, targetGridPos.y);
            if (targetTile != null)
            {
                tilesToHighlight.Add(targetTile);
            }
        }

        DeployableManager.Instance.HighlightTiles(tilesToHighlight, DeployableManager.Instance.attackRangeTileColor);
    }

    /// <summary>
    /// 현재 타겟의 유효성 검사 : currentTarget이 공격 범위 내에 없다면 제거함
    /// </summary>
    private void ValidateCurrentTarget()
    {
        if(currentTarget != null)
        {
            if (!IsTargetInRange(currentTarget))
            {
                currentTarget.RemoveAttackingOperator(this);
                currentTarget = null;
            }
        }
    }

    protected override bool IsTargetInRange(Unit unit)
    {
        if (unit is Enemy) // 타입 매칭은 `is`를 사용
        {
            Vector2Int enemyGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(unit.transform.position);
            Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position); 

            foreach (Vector2Int offset in data.attackableTiles)
            {
                Vector2Int rotatedOffset = RotateOffset(offset, facingDirection);
                Vector2Int targetGridPos = operatorGridPos + rotatedOffset;

                if (targetGridPos == enemyGridPos)
                {
                    return true;
                }
            }

            // 공격 범위에 없다
            return false; 

        }

        // Enemy가 아니다
        return false;
    }


    public override void Deploy(Vector3 position)
    {
        base.Deploy(position);

        maxHealth = data.currentStats.health;
        SetDirection(facingDirection);
        CreateOperatorBarUI();

        ShowDirectionIndicator(true);
    }

    public override void Retreat()
    {
        base.Retreat();
    }

    // ICombatEntity 메서드 - 인터페이스 멤버는 모두 public으로 구현해야 함
    public bool CanAttack()
    {
        return currentTarget != null && AttackCooldown <= 0;
    }

    public void SetAttackCooldown()
    {
        AttackCooldown = 1 / currentStats.attackSpeed;
    }

    public void UpdateAttackCooldown()
    {
        if (AttackCooldown > 0f)
        {
            AttackCooldown -= Time.deltaTime;
        }
    }
}
