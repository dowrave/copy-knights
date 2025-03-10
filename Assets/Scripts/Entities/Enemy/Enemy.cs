using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static ICombatEntity;
using System;


public class Enemy : UnitEntity, IMovable, ICombatEntity, ICrowdControlTarget
{
    [SerializeField]
    private EnemyData enemyData = default!;
    public EnemyData BaseData => enemyData;

    private EnemyStats currentStats;

    public AttackType AttackType => enemyData.attackType;
    public AttackRangeType AttackRangeType => enemyData.attackRangeType;
    public float AttackPower { get => currentStats.AttackPower; private set => currentStats.AttackPower = value; }
    public float AttackSpeed { get => currentStats.AttackSpeed; private set => currentStats.AttackSpeed = value; }
    public float MovementSpeed { get => currentStats.MovementSpeed; }
    public int BlockCount { get => enemyData.blockCount; private set => enemyData.blockCount = value; } // Enemy가 차지하는 저지 수
    public float AttackCooldown { get; private set; } // 다음 공격까지의 대기 시간
    public float AttackDuration { get; private set; } // 공격 모션 시간. Animator가 추가될 때 수정 필요할 듯. 항상 Cooldown보다 짧아야 함.

    public float AttackRange
    {
        get
        {
            return enemyData.attackRangeType == AttackRangeType.Melee ? 0f : currentStats.AttackRange;
        }
        private set
        {
            currentStats.AttackRange = value;
        }
    }

    // 경로 관련
    private PathData? pathData;
    private List<Vector3> currentPath = new List<Vector3>();
    private List<UnitEntity> targetsInRange = new List<UnitEntity>();
    private PathNode nextNode = default!;
    private int nextNodeIndex = 0; // 시작하자마자 1이 됨
    private Vector3 nextPosition; // 다음 노드의 좌표
    private Vector3 destinationPosition; // 목적지
    private bool isWaiting = false;
    private Barricade? targetBarricade;

    private Operator? blockingOperator; // 자신을 저지 중인 오퍼레이터
    public Operator? BlockingOperator => blockingOperator;
    public UnitEntity? CurrentTarget { get; private set; } // 공격 대상

    protected int initialPoolSize = 5;
    protected string? projectileTag;

    // 이펙트 풀 태그
    string? meleeAttackEffectTag;
    string hitEffectTag = string.Empty;

    [SerializeField] private GameObject enemyBarUIPrefab = default!;
    private EnemyBarUI? enemyBarUI;

    // 접촉 중인 타일 관리
    private List<Tile> contactedTiles = new List<Tile>();

    // 메쉬의 회전 관련해서 모델 관리
    [Header("Model Components")]
    [SerializeField] protected GameObject modelContainer = default!;

    // ICrowdControlTarget
    public Vector3 Position => transform.position;


    protected override void Awake()
    {
        Faction = Faction.Enemy;

        InitializeModelComponents();

        base.Awake();
    }

    // 모델 회전 관련 로직을 쓸 일이 Enemy 뿐이라 여기에 구현해놓음.
    protected void InitializeModelComponents()
    {
        if (modelContainer == null)
        {
            modelContainer = transform.Find("ModelContainer").gameObject;
        }
    }

    public void Initialize(EnemyData enemyData, PathData pathData)
    {
        this.enemyData = enemyData;
        currentStats = enemyData.stats;
        this.pathData = pathData;

        InitializeHP();

        // 프리팹 설정
        Prefab = enemyData.prefab;

        SetupInitialPosition();
        CreateEnemyBarUI();
        UpdateNextNode();
        InitializeCurrentPath();

        // 최초에 설정한 경로가 막힌 상황일 때 동작
        if (PathfindingManager.Instance!.IsBarricadeDeployed && IsPathBlocked())
        {
            FindPathToDestinationOrBarricade();
        }

        CreateObjectPool();
    }

    private void OnEnable()
    {
        Barricade.OnBarricadeDeployed += OnBarricadePlaced;
        Barricade.OnBarricadeRemoved += OnBarricadeRemovedWithDelay;
    }

    private void OnDisable()
    {
        Barricade.OnBarricadeDeployed -= OnBarricadePlaced;
        Barricade.OnBarricadeRemoved -= OnBarricadeRemovedWithDelay;
    }

    private void SetupInitialPosition()
    {
        if (pathData == null || pathData.nodes == null) return;

        if (pathData!.nodes!.Count > 0)
        {
            transform.position = MapManager.Instance!.ConvertToWorldPosition(pathData.nodes![0].gridPosition) +
                Vector3.up * BaseData.defaultYPosition;
        }
    }

    protected void Update()
    {
        if (StageManager.Instance!.currentState == GameState.Battle)
        {
            UpdateAttackDuration();
            UpdateAttackCooldown();
            UpdateCrowdControls();

            if (pathData == null || pathData.nodes == null) return;

            if (nextNodeIndex < pathData.nodes.Count)
            {
                if (activeCC.Any(cc => cc is StunEffect)) return; // 스턴
                if (AttackDuration > 0) return;  // 공격 모션 중

                // 공격 범위 내의 적 리스트 & 현재 공격 대상 갱신
                SetCurrentTarget();

                // 저지당함 - 근거리 공격
                if (blockingOperator != null)
                {
                    if (AttackCooldown <= 0)
                    {
                        PerformMeleeAttack(CurrentTarget!, AttackPower); 
                    }
                }
                else
                {
                    // 바리케이트가 타겟일 경우
                    if (targetBarricade != null && Vector3.Distance(transform.position, targetBarricade.transform.position) < 0.5f)
                    {
                        PerformMeleeAttack(targetBarricade, AttackPower);
                    }

                    // 타겟이 있고, 공격이 가능한 상태
                    if (CanAttack())
                    {
                        Attack(CurrentTarget!, AttackPower);
                    }

                    // 이동 관련 로직.
                    else if (!isWaiting)
                    {
                        MoveAlongPath(); // 이동
                    }
                }
            }
        }
        
    }


    // pathData.nodes를 이용해 currentPath 초기화
    private void InitializeCurrentPath()
    {
        if (pathData == null || pathData.nodes == null) throw new InvalidOperationException("null인 변수가 존재");

        foreach (var node in pathData.nodes)
        {
            currentPath.Add(MapManager.Instance!.ConvertToWorldPosition(node.gridPosition) + Vector3.up * BaseData.defaultYPosition);
        }

        destinationPosition = currentPath[currentPath.Count - 1]; // 목적지 설정
    }

    // 경로를 따라 이동
    private void MoveAlongPath()
    {
        if (nextPosition == null || destinationPosition == null) throw new InvalidOperationException("다음/목적지 노드가 설정되어있지 않음");

        if (CheckIfReachedDestination())
        {
            ReachDestination();
            return;
        }
        
        Move(nextPosition);
        RotateModelTowardsMovementDirection();

        // 노드 도달 확인
        if (Vector3.Distance(transform.position, nextPosition) < 0.05f)
        {
            // 목적지 도달
            if (Vector3.Distance(transform.position, destinationPosition) < 0.05f)
            {
                ReachDestination();
            }
            // 기다려야 하는 경우
            else if (nextNode.waitTime > 0)
            {
                StartCoroutine(WaitAtNode(nextNode.waitTime));
            }
            // 노드 업데이트
            else
            {
                UpdateNextNode();
            }
        }
    }

    public void Move(Vector3 destination)
    {
        transform.position = Vector3.MoveTowards(transform.position, destination, MovementSpeed * Time.deltaTime);
    }

    // 대기 중일 때 실행
    private IEnumerator WaitAtNode(float waitTime)
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);
        isWaiting = false;

        UpdateNextNode();
    }

    // 다음 노드 인덱스를 설정하고 현재 목적지로 지정함
    private void UpdateNextNode()
    {
        if (pathData == null || pathData.nodes == null) return;

        nextNodeIndex++;
        if (nextNodeIndex < pathData.nodes.Count)
        {
            nextNode = pathData.nodes[nextNodeIndex];
            nextPosition = MapManager.Instance!.ConvertToWorldPosition(nextNode.gridPosition) + 
                Vector3.up * BaseData.defaultYPosition;
        }
    }

    private void ReachDestination()
    {
        StageManager.Instance!.OnEnemyReachDestination();
        Destroy(gameObject);
    }

    //  공격 범위 내의 오퍼레이터 리스트 업데이트
    public void UpdateTargetsInRange()
    {
        targetsInRange.Clear();

        // 판정용 콜라이더 처리
        Collider[] colliders = Physics.OverlapSphere(transform.position, AttackRange); 

        foreach (var collider in colliders)
        {
            DeployableUnitEntity? target = collider.transform.parent?.GetComponent<DeployableUnitEntity>(); // GetComponent : 해당 오브젝트부터 시작, 부모 오브젝트로 올라가며 컴포넌트를 찾는다.
            if (target != null && target.IsDeployed && Faction.Ally == target.Faction)
            {
                // 실제 거리 계산
                float actualDistance = Vector3.Distance(transform.position, target.transform.position);
                if (actualDistance <= AttackRange)
                {
                    targetsInRange.Add(target);
                }
            }
        }
    }

    public void Attack(UnitEntity target, float damage)
    {
        PerformAttack(target, damage);
    }

    private void PerformAttack(UnitEntity target, float damage)
    {
        switch (AttackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target, damage);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target, damage);
                break;
        }
    }

    private void PerformMeleeAttack(UnitEntity target, float damage)
    {
        SetAttackTimings(); // 이걸 따로 호출하는 경우가 있어서 여기서 다시 설정
        AttackSource attackSource = new AttackSource(transform.position, false, BaseData.HitEffectPrefab);

        PlayMeleeAttackEffect(target, attackSource);

        target.TakeDamage(this, attackSource, damage);
    }

    private void PerformRangedAttack(UnitEntity target, float damage)
    {
        SetAttackTimings();
        if (BaseData.projectilePrefab != null && projectileTag != null)
        {
            // 투사체 생성 위치
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
            GameObject? projectileObj = ObjectPoolManager.Instance!.SpawnFromPool(projectileTag, spawnPosition, Quaternion.identity);

            if (projectileObj != null)
            {
                Projectile? projectile = projectileObj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(this, target, damage, false, projectileTag, BaseData.hitEffectPrefab);
                }
            }
        }
    }

    protected bool IsTargetInRange(UnitEntity target)
    {
        return Vector3.Distance(target.transform.position, transform.position) <= AttackRange;
    }

    protected override void Die()
    {
        // 저지 중인 오퍼레이터에게 저지를 해제시킴
        if (blockingOperator != null)
        {
            blockingOperator.UnblockEnemy(this);
        }

        // 공격 중인 개체의 현재 타겟 제거
        foreach (Operator op in attackingEntities.ToList())
        {
            op.OnTargetLost(this);
        }

        // 접촉 중인 타일들에서 이 개체 제거
        foreach (Tile tile in contactedTiles)
        {
            tile.EnemyExited(this);
        }

        StageManager.Instance!.OnEnemyDefeated(); // 사망한 적 수 +1
        Debug.Log($"{BaseData.entityName} 사망, 사망 카운트 + 1");

        // 공격 이펙트 프리팹 제거
        if (BaseData.hitEffectPrefab != null)
        {
            ObjectPoolManager.Instance!.RemovePool("Effect_" + BaseData.entityName);
        }

        // UI 제거
        if (enemyBarUI != null)
        {
            Destroy(enemyBarUI.gameObject);
        }

        base.Die();
    }

    public override void TakeDamage(UnitEntity attacker, AttackSource attackSource, float damage)
    {
        if (attacker is ICombatEntity iCombatEntity && CurrentHealth > 0) 
        {
            // 방어 / 마법 저항력이 고려된 실제 들어오는 대미지
            float actualDamage = CalculateActualDamage(iCombatEntity.AttackType, damage);

            // 쉴드를 깎고 남은 대미지
            float remainingDamage = shieldSystem.AbsorbDamage(actualDamage);

            // 체력 계산
            CurrentHealth = Mathf.Max(0, CurrentHealth - remainingDamage);

            // attacker가 null일 때에도 잘 동작합니다
            if (attacker is Operator op)
            {
                StatisticsManager.Instance!.UpdateDamageDealt(op.OperatorData, actualDamage);

                if (op.OperatorData.hitEffectPrefab != null)
                {
                    PlayGetHitEffect(attacker, attackSource);
                }
            }
        }


        if (CurrentHealth <= 0)
        {
            Die();
        }

        // UI 업데이트
        if (enemyBarUI != null)
        {
            enemyBarUI.UpdateUI();
        }
    }

    public void UnblockFrom(Operator op)
    {
        if (blockingOperator == op)
        {
            blockingOperator = null;
        }
    }

    // 마지막 타일의 월드 좌표 기준
    private bool CheckIfReachedDestination()
    {
        if (pathData == null || pathData.nodes == null) throw new InvalidOperationException("pathData, pathData.nodes가 없음");

        if (pathData.nodes.Count == 0) return false;

        Vector2Int lastNodeGridPos = pathData!.nodes![pathData!.nodes!.Count - 1].gridPosition;
        Vector3 lastNodePosition = MapManager.Instance!.ConvertToWorldPosition(lastNodeGridPos) + Vector3.up * 0.5f;

        return Vector3.Distance(transform.position, lastNodePosition) < 0.05f;
    }

    // Enemy가 공격할 대상 지정
    public void SetCurrentTarget()
    {
        // 저지를 당할 때는 자신을 저지하는 객체를 타겟으로 지정
        if (blockingOperator != null)
        {
            CurrentTarget = blockingOperator;
            NotifyTarget();
            return;
        }

        // 저지가 아니라면 공격 범위 내의 가장 마지막에 배치된 적 공격
        UpdateTargetsInRange(); // 공격 범위 내의 Operator 리스트 갱신

        if (targetsInRange.Count > 0)
        {
            // 다른 오브젝트를 공격해야 할 수도 있는데 지금은 일단 오퍼레이터에 한정해서 구현함
            CurrentTarget = targetsInRange
                .OfType<Operator>()
                .OrderByDescending(o => o.DeploymentOrder)
                .FirstOrDefault();

            NotifyTarget();
            return;
        }

        // 저지당하고 있지 않고, 공격 범위 내에도 타겟이 없다면 
        RemoveCurrentTarget();
    }

    public void RemoveCurrentTarget()
    {
        if (CurrentTarget != null)
        {
            CurrentTarget = null;
        }
    }


    // CurrentTarget에게 자신이 공격하고 있음을 알림
    public void NotifyTarget()
    {
        CurrentTarget?.AddAttackingEntity(this);
    }

    public void InitializeProjectilePool()
    {
        if (AttackRangeType == AttackRangeType.Ranged && BaseData.projectilePrefab != null)
        {
            projectileTag = $"{BaseData.entityName}_Projectile";
            ObjectPoolManager.Instance!.CreatePool(projectileTag, BaseData.projectilePrefab, initialPoolSize);
        }
    }


    // 현재 경로상에서 목적지까지 남은 거리 계산
    public float GetRemainingPathDistance()
    {
        if (currentPath.Count == 0 || nextNodeIndex > +currentPath.Count)
        {
            return float.MaxValue;
        }

        float distance = 0f;
        for (int i = nextNodeIndex; i < currentPath.Count - 1; i++)
        {
            // 첫 타일에 한해서만 현재 위치를 기반으로 계산(여러 Enemy가 같은 타일에 있을 수 있기 때문)
            if (i == nextNodeIndex)
            {
                Vector3 nowPosition = new Vector3(transform.position.x, 0f, transform.position.z);
                distance += Vector3.Distance(nowPosition, currentPath[i + 1]);
            }

            distance += Vector3.Distance(currentPath[i], currentPath[i + 1]);
        }

        return distance;
    }


    // Barricade 설치 시 현재 경로가 막혔다면 재계산
    private void OnBarricadePlaced(Barricade barricade)
    {
        // 내 타일과 같은 타일에 바리케이드가 배치된 경우
        if (barricade.CurrentTile != null && 
            barricade.CurrentTile.EnemiesOnTile.Contains(this))
        {
            targetBarricade = barricade;
        }

        // 현재 사용 중인 경로가 막힌 경우
        else if (IsPathBlocked())
        {
            FindPathToDestinationOrBarricade();
        }
    }

    private void OnBarricadeRemovedWithDelay(Barricade barricade)
    {
        StartCoroutine(OnBarricadeRemoved(barricade));
    }

    // 바리케이드 제거 시 동작
    private IEnumerator OnBarricadeRemoved(Barricade barricade)
    {
        // 바리케이드가 파괴될 시간 확보
        yield return new WaitForSeconds(0.1f); 
        
        // 바리케이드와 관계 없던 Enemy는 경로를 다시 탐색
        if (targetBarricade == null)  
        {
            FindPathToDestinationOrBarricade();
        }

        // 해당 바리케이드가 목표였던 Enemy들의 targetBarricade 해제
        else if (targetBarricade == barricade)
        {
            targetBarricade = null;
            FindPathToDestinationOrBarricade();
        }

        // 다른 바리케이드를 목표로 하고 있다면 별도 동작 X
    }

    // 현재 pathData를 사용하는 경로가 막혔는지를 점검한다
    private bool IsPathBlocked()
    {
        if (currentPath.Count == 0) throw new InvalidOperationException("currentPath가 비어 있음");
        
        for (int i = nextNodeIndex; i <= currentPath.Count - 1; i++)
        {
            // 경로가 막힌 상황 : 기존 경로 데이터들을 정리한다
            if ((i == nextNodeIndex && PathfindingManager.Instance!.IsPathSegmentValid(transform.position, currentPath[i]) == false) ||
                PathfindingManager.Instance!.IsPathSegmentValid(currentPath[i], currentPath[i + 1]) == false)
            {
                pathData = null;
                currentPath.Clear();
                Debug.Log("현재 경로가 막힘");
                return true;
            }
        }
       

        return false;
    }


    // CalculatePath로 탐색된 경로를 받아와 pathData와 currentPath 초기화
    private void SetNewPath(List<PathNode> newPathNodes)
    {
        if (newPathNodes != null && newPathNodes.Count > 0)
        {
            PathData newPathData = ScriptableObject.CreateInstance<PathData>();
            newPathData.nodes = newPathNodes;
            pathData = newPathData;
            currentPath = newPathNodes.Select(node => MapManager.Instance!.ConvertToWorldPosition(node.gridPosition) + Vector3.up * 0.5f).ToList();

            nextNodeIndex = 0;

            UpdateNextNode();
        }
    }

    // targetPosition으로 향하는 경로를 계산하고, 경로가 있다면 새로운 pathData와 currentPath로 설정함
    private bool CalculateAndSetPath(Vector3 currentPosition, Vector3 targetPosition)
    {
        List<PathNode>? tempPathNodes = PathfindingManager.Instance!.FindPathAsNodes(currentPosition, targetPosition);

        if (tempPathNodes == null || tempPathNodes.Count == 0) return false; // 목적지로 향하는 경로가 없음

        SetNewPath(tempPathNodes);
        return true;
    }

    // 현재 위치에서 가장 가까운 바리케이드를 설정하고, 바리케이드로 향하는 경로를 설정함
    private void SetBarricadePath()
    {
        targetBarricade = PathfindingManager.Instance!.GetNearestBarricade(transform.position);

        if (targetBarricade != null)
        {
            CalculateAndSetPath(transform.position, targetBarricade.transform.position);
        }
    }

    // 목적지로 향하는 경로를 찾고, 없다면 가장 가까운 바리케이드로 향하는 경로를 설정함
    private void FindPathToDestinationOrBarricade()
    {
        if (!CalculateAndSetPath(transform.position, destinationPosition))
        {
            Debug.Log("목적지로 향하는 경로 발견 및 설정");
            SetBarricadePath();
        }
    }


    // 인터페이스 때문에 구현
    public void UpdateAttackDuration()
    {
        if (AttackDuration > 0f)
        {
            AttackDuration -= Time.deltaTime;
        }
    }

    public void UpdateAttackCooldown()
    {
        if (AttackCooldown > 0f)
        {
            AttackCooldown -= Time.deltaTime;
        }
    }


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

    public void SetAttackDuration()
    {
        AttackDuration = 0.3f / AttackSpeed;
    }

    public void SetAttackCooldown(float? intentionalCooldown = null)
    {
        AttackCooldown = 1 / AttackSpeed;
    }

    public bool CanAttack()
    {
        return CurrentTarget != null &&
            AttackCooldown <= 0 &&
            AttackDuration <= 0;
    }

    private void CreateObjectPool()
    {
        // 다른 객체는 다른 태그를 가져야 함
        string id = GetInstanceID().ToString();

        // 근접 공격 이펙트 풀 생성
        if (BaseData.meleeAttackEffectPrefab != null)
        {
            meleeAttackEffectTag = id + BaseData.entityName + BaseData.meleeAttackEffectPrefab.name;
            ObjectPoolManager.Instance!.CreatePool(
                meleeAttackEffectTag,
                BaseData.meleeAttackEffectPrefab
            );
        }

        // 피격 이펙트 풀 생성
        if (BaseData.hitEffectPrefab != null)
        {
            hitEffectTag = id + BaseData.entityName + BaseData.hitEffectPrefab.name;
            ObjectPoolManager.Instance!.CreatePool(
                hitEffectTag,
                BaseData.hitEffectPrefab
            );
        }

        InitializeProjectilePool();
    }

    private void PlayMeleeAttackEffect(UnitEntity target, AttackSource attackSource)
    {
        // 이펙트 처리
        if (meleeAttackEffectTag != null && BaseData.meleeAttackEffectPrefab != null)
        {
            GameObject? effectObj = ObjectPoolManager.Instance!.SpawnFromPool(
                   meleeAttackEffectTag,
                   transform.position,
                   Quaternion.identity
            );
            if (effectObj != null)
            {
                CombatVFXController? combatVFXController = effectObj.GetComponent<CombatVFXController>();
                if (combatVFXController != null)
                {
                    combatVFXController.Initialize(attackSource, target, meleeAttackEffectTag);
                }
            }
        }
    }

    private void CreateEnemyBarUI()
    {
        if (enemyBarUIPrefab != null)
        {
            GameObject uiObject = Instantiate(enemyBarUIPrefab, transform);
            enemyBarUI = uiObject.GetComponentInChildren<EnemyBarUI>();
            if (enemyBarUI != null)
            {
                enemyBarUI.Initialize(this);
            }
        }
    }


    private void RemoveProjectilePool()
    {
        if (AttackRangeType == AttackRangeType.Ranged && !string.IsNullOrEmpty(projectileTag))
        {
            ObjectPoolManager.Instance!.RemovePool(projectileTag);
        }
    }

    private void RemoveObjectPool()
    {
        if (meleeAttackEffectTag != null)
        {
            ObjectPoolManager.Instance!.RemovePool(meleeAttackEffectTag);
        }

        if (hitEffectTag != null)
        {
            ObjectPoolManager.Instance!.RemovePool(hitEffectTag);
        }

        RemoveProjectilePool();
    }

    protected override void InitializeHP()
    {
        MaxHealth = currentStats.Health;
        CurrentHealth = MaxHealth;
    }

    public void SetBlockingOperator(Operator op)
    {
        if (blockingOperator == null)
        {
            blockingOperator = op;
        }
    }

    public void RemoveBlockingOperator()
    {
        blockingOperator = null;
    }

    protected override float CalculateActualDamage(AttackType attacktype, float incomingDamage)
    {
        float actualDamage = 0; // 할당까지 필수

        switch (attacktype)
        {
            case AttackType.Physical:
                actualDamage = incomingDamage - currentStats.Defense;
                break;
            case AttackType.Magical:
                actualDamage = incomingDamage * (1 - currentStats.MagicResistance / 100);
                break;
            case AttackType.True:
                actualDamage = incomingDamage;
                break;
        }

        return Mathf.Max(actualDamage, 0.05f * incomingDamage); // 들어온 대미지의 5%는 들어가게끔 보장
    }

    private void OnTriggerEnter(Collider other)
    {
        Tile tile = other.GetComponent<Tile>();
        if (tile != null)
        {
            contactedTiles.Add(tile);
            tile.EnemyEntered(this);
        }
    }


    private void OnTriggerExit(Collider other)
    {
        Tile tile = other.GetComponent<Tile>();
        if (tile != null && contactedTiles.Contains(tile))
        {
            tile.EnemyExited(this);
            contactedTiles.Remove(tile);
        }
    }

    // enemy는 콜라이더를 끌 상황이 없는 듯 하다
    protected override void SetColliderState()
    {
        boxCollider.enabled = true;
    }

    public void SetMovementSpeed(float newSpeed)
    {
        currentStats.MovementSpeed = newSpeed;
    }

    private void RotateModelTowardsMovementDirection()
    {
        if (modelContainer == null) return;

        Vector3 direction = nextPosition - transform.position;
        direction.y = 0f; 
        if (direction != Vector3.zero)
        {
            
            // 핵심 : LookRoation은 +z 방향을 바라보게 만든다
            // forward : 바라볼 방향 / up : 윗 방향
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            modelContainer.transform.rotation = targetRotation;

            // 다른 방법
            //direction.Normalize();
            //float angle = Vector3.SignedAngle(modelContainer.transform.forward, direction, Vector3.up);
            //modelContainer.transform.eulerAngles = new Vector3(0, angle, 0);

            // 만약 부드러운 회전을 원한다면
            //model.transform.rotation = Quaternion.Slerp(
            //    model.transform.rotation, 
            //    targetRotation, 
            //    rotationSpeed * Time.deltaTime
            //    );
        }
    }


    protected void OnDestroy()
    {
        RemoveObjectPool();
    }

}

