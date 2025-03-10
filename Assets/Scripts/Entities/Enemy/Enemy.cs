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
    public int BlockCount { get => enemyData.blockCount; private set => enemyData.blockCount = value; } // Enemy�� �����ϴ� ���� ��
    public float AttackCooldown { get; private set; } // ���� ���ݱ����� ��� �ð�
    public float AttackDuration { get; private set; } // ���� ��� �ð�. Animator�� �߰��� �� ���� �ʿ��� ��. �׻� Cooldown���� ª�ƾ� ��.

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

    // ��� ����
    private PathData? pathData;
    private List<Vector3> currentPath = new List<Vector3>();
    private List<UnitEntity> targetsInRange = new List<UnitEntity>();
    private PathNode nextNode = default!;
    private int nextNodeIndex = 0; // �������ڸ��� 1�� ��
    private Vector3 nextPosition; // ���� ����� ��ǥ
    private Vector3 destinationPosition; // ������
    private bool isWaiting = false;
    private Barricade? targetBarricade;

    private Operator? blockingOperator; // �ڽ��� ���� ���� ���۷�����
    public Operator? BlockingOperator => blockingOperator;
    public UnitEntity? CurrentTarget { get; private set; } // ���� ���

    protected int initialPoolSize = 5;
    protected string? projectileTag;

    // ����Ʈ Ǯ �±�
    string? meleeAttackEffectTag;
    string hitEffectTag = string.Empty;

    [SerializeField] private GameObject enemyBarUIPrefab = default!;
    private EnemyBarUI? enemyBarUI;

    // ���� ���� Ÿ�� ����
    private List<Tile> contactedTiles = new List<Tile>();

    // �޽��� ȸ�� �����ؼ� �� ����
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

    // �� ȸ�� ���� ������ �� ���� Enemy ���̶� ���⿡ �����س���.
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

        // ������ ����
        Prefab = enemyData.prefab;

        SetupInitialPosition();
        CreateEnemyBarUI();
        UpdateNextNode();
        InitializeCurrentPath();

        // ���ʿ� ������ ��ΰ� ���� ��Ȳ�� �� ����
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
                if (activeCC.Any(cc => cc is StunEffect)) return; // ����
                if (AttackDuration > 0) return;  // ���� ��� ��

                // ���� ���� ���� �� ����Ʈ & ���� ���� ��� ����
                SetCurrentTarget();

                // �������� - �ٰŸ� ����
                if (blockingOperator != null)
                {
                    if (AttackCooldown <= 0)
                    {
                        PerformMeleeAttack(CurrentTarget!, AttackPower); 
                    }
                }
                else
                {
                    // �ٸ�����Ʈ�� Ÿ���� ���
                    if (targetBarricade != null && Vector3.Distance(transform.position, targetBarricade.transform.position) < 0.5f)
                    {
                        PerformMeleeAttack(targetBarricade, AttackPower);
                    }

                    // Ÿ���� �ְ�, ������ ������ ����
                    if (CanAttack())
                    {
                        Attack(CurrentTarget!, AttackPower);
                    }

                    // �̵� ���� ����.
                    else if (!isWaiting)
                    {
                        MoveAlongPath(); // �̵�
                    }
                }
            }
        }
        
    }


    // pathData.nodes�� �̿��� currentPath �ʱ�ȭ
    private void InitializeCurrentPath()
    {
        if (pathData == null || pathData.nodes == null) throw new InvalidOperationException("null�� ������ ����");

        foreach (var node in pathData.nodes)
        {
            currentPath.Add(MapManager.Instance!.ConvertToWorldPosition(node.gridPosition) + Vector3.up * BaseData.defaultYPosition);
        }

        destinationPosition = currentPath[currentPath.Count - 1]; // ������ ����
    }

    // ��θ� ���� �̵�
    private void MoveAlongPath()
    {
        if (nextPosition == null || destinationPosition == null) throw new InvalidOperationException("����/������ ��尡 �����Ǿ����� ����");

        if (CheckIfReachedDestination())
        {
            ReachDestination();
            return;
        }
        
        Move(nextPosition);
        RotateModelTowardsMovementDirection();

        // ��� ���� Ȯ��
        if (Vector3.Distance(transform.position, nextPosition) < 0.05f)
        {
            // ������ ����
            if (Vector3.Distance(transform.position, destinationPosition) < 0.05f)
            {
                ReachDestination();
            }
            // ��ٷ��� �ϴ� ���
            else if (nextNode.waitTime > 0)
            {
                StartCoroutine(WaitAtNode(nextNode.waitTime));
            }
            // ��� ������Ʈ
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

    // ��� ���� �� ����
    private IEnumerator WaitAtNode(float waitTime)
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);
        isWaiting = false;

        UpdateNextNode();
    }

    // ���� ��� �ε����� �����ϰ� ���� �������� ������
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

    //  ���� ���� ���� ���۷����� ����Ʈ ������Ʈ
    public void UpdateTargetsInRange()
    {
        targetsInRange.Clear();

        // ������ �ݶ��̴� ó��
        Collider[] colliders = Physics.OverlapSphere(transform.position, AttackRange); 

        foreach (var collider in colliders)
        {
            DeployableUnitEntity? target = collider.transform.parent?.GetComponent<DeployableUnitEntity>(); // GetComponent : �ش� ������Ʈ���� ����, �θ� ������Ʈ�� �ö󰡸� ������Ʈ�� ã�´�.
            if (target != null && target.IsDeployed && Faction.Ally == target.Faction)
            {
                // ���� �Ÿ� ���
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
        SetAttackTimings(); // �̰� ���� ȣ���ϴ� ��찡 �־ ���⼭ �ٽ� ����
        AttackSource attackSource = new AttackSource(transform.position, false, BaseData.HitEffectPrefab);

        PlayMeleeAttackEffect(target, attackSource);

        target.TakeDamage(this, attackSource, damage);
    }

    private void PerformRangedAttack(UnitEntity target, float damage)
    {
        SetAttackTimings();
        if (BaseData.projectilePrefab != null && projectileTag != null)
        {
            // ����ü ���� ��ġ
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
        // ���� ���� ���۷����Ϳ��� ������ ������Ŵ
        if (blockingOperator != null)
        {
            blockingOperator.UnblockEnemy(this);
        }

        // ���� ���� ��ü�� ���� Ÿ�� ����
        foreach (Operator op in attackingEntities.ToList())
        {
            op.OnTargetLost(this);
        }

        // ���� ���� Ÿ�ϵ鿡�� �� ��ü ����
        foreach (Tile tile in contactedTiles)
        {
            tile.EnemyExited(this);
        }

        StageManager.Instance!.OnEnemyDefeated(); // ����� �� �� +1
        Debug.Log($"{BaseData.entityName} ���, ��� ī��Ʈ + 1");

        // ���� ����Ʈ ������ ����
        if (BaseData.hitEffectPrefab != null)
        {
            ObjectPoolManager.Instance!.RemovePool("Effect_" + BaseData.entityName);
        }

        // UI ����
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
            // ��� / ���� ���׷��� ����� ���� ������ �����
            float actualDamage = CalculateActualDamage(iCombatEntity.AttackType, damage);

            // ���带 ��� ���� �����
            float remainingDamage = shieldSystem.AbsorbDamage(actualDamage);

            // ü�� ���
            CurrentHealth = Mathf.Max(0, CurrentHealth - remainingDamage);

            // attacker�� null�� ������ �� �����մϴ�
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

        // UI ������Ʈ
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

    // ������ Ÿ���� ���� ��ǥ ����
    private bool CheckIfReachedDestination()
    {
        if (pathData == null || pathData.nodes == null) throw new InvalidOperationException("pathData, pathData.nodes�� ����");

        if (pathData.nodes.Count == 0) return false;

        Vector2Int lastNodeGridPos = pathData!.nodes![pathData!.nodes!.Count - 1].gridPosition;
        Vector3 lastNodePosition = MapManager.Instance!.ConvertToWorldPosition(lastNodeGridPos) + Vector3.up * 0.5f;

        return Vector3.Distance(transform.position, lastNodePosition) < 0.05f;
    }

    // Enemy�� ������ ��� ����
    public void SetCurrentTarget()
    {
        // ������ ���� ���� �ڽ��� �����ϴ� ��ü�� Ÿ������ ����
        if (blockingOperator != null)
        {
            CurrentTarget = blockingOperator;
            NotifyTarget();
            return;
        }

        // ������ �ƴ϶�� ���� ���� ���� ���� �������� ��ġ�� �� ����
        UpdateTargetsInRange(); // ���� ���� ���� Operator ����Ʈ ����

        if (targetsInRange.Count > 0)
        {
            // �ٸ� ������Ʈ�� �����ؾ� �� ���� �ִµ� ������ �ϴ� ���۷����Ϳ� �����ؼ� ������
            CurrentTarget = targetsInRange
                .OfType<Operator>()
                .OrderByDescending(o => o.DeploymentOrder)
                .FirstOrDefault();

            NotifyTarget();
            return;
        }

        // �������ϰ� ���� �ʰ�, ���� ���� ������ Ÿ���� ���ٸ� 
        RemoveCurrentTarget();
    }

    public void RemoveCurrentTarget()
    {
        if (CurrentTarget != null)
        {
            CurrentTarget = null;
        }
    }


    // CurrentTarget���� �ڽ��� �����ϰ� ������ �˸�
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


    // ���� ��λ󿡼� ���������� ���� �Ÿ� ���
    public float GetRemainingPathDistance()
    {
        if (currentPath.Count == 0 || nextNodeIndex > +currentPath.Count)
        {
            return float.MaxValue;
        }

        float distance = 0f;
        for (int i = nextNodeIndex; i < currentPath.Count - 1; i++)
        {
            // ù Ÿ�Ͽ� ���ؼ��� ���� ��ġ�� ������� ���(���� Enemy�� ���� Ÿ�Ͽ� ���� �� �ֱ� ����)
            if (i == nextNodeIndex)
            {
                Vector3 nowPosition = new Vector3(transform.position.x, 0f, transform.position.z);
                distance += Vector3.Distance(nowPosition, currentPath[i + 1]);
            }

            distance += Vector3.Distance(currentPath[i], currentPath[i + 1]);
        }

        return distance;
    }


    // Barricade ��ġ �� ���� ��ΰ� �����ٸ� ����
    private void OnBarricadePlaced(Barricade barricade)
    {
        // �� Ÿ�ϰ� ���� Ÿ�Ͽ� �ٸ����̵尡 ��ġ�� ���
        if (barricade.CurrentTile != null && 
            barricade.CurrentTile.EnemiesOnTile.Contains(this))
        {
            targetBarricade = barricade;
        }

        // ���� ��� ���� ��ΰ� ���� ���
        else if (IsPathBlocked())
        {
            FindPathToDestinationOrBarricade();
        }
    }

    private void OnBarricadeRemovedWithDelay(Barricade barricade)
    {
        StartCoroutine(OnBarricadeRemoved(barricade));
    }

    // �ٸ����̵� ���� �� ����
    private IEnumerator OnBarricadeRemoved(Barricade barricade)
    {
        // �ٸ����̵尡 �ı��� �ð� Ȯ��
        yield return new WaitForSeconds(0.1f); 
        
        // �ٸ����̵�� ���� ���� Enemy�� ��θ� �ٽ� Ž��
        if (targetBarricade == null)  
        {
            FindPathToDestinationOrBarricade();
        }

        // �ش� �ٸ����̵尡 ��ǥ���� Enemy���� targetBarricade ����
        else if (targetBarricade == barricade)
        {
            targetBarricade = null;
            FindPathToDestinationOrBarricade();
        }

        // �ٸ� �ٸ����̵带 ��ǥ�� �ϰ� �ִٸ� ���� ���� X
    }

    // ���� pathData�� ����ϴ� ��ΰ� ���������� �����Ѵ�
    private bool IsPathBlocked()
    {
        if (currentPath.Count == 0) throw new InvalidOperationException("currentPath�� ��� ����");
        
        for (int i = nextNodeIndex; i <= currentPath.Count - 1; i++)
        {
            // ��ΰ� ���� ��Ȳ : ���� ��� �����͵��� �����Ѵ�
            if ((i == nextNodeIndex && PathfindingManager.Instance!.IsPathSegmentValid(transform.position, currentPath[i]) == false) ||
                PathfindingManager.Instance!.IsPathSegmentValid(currentPath[i], currentPath[i + 1]) == false)
            {
                pathData = null;
                currentPath.Clear();
                Debug.Log("���� ��ΰ� ����");
                return true;
            }
        }
       

        return false;
    }


    // CalculatePath�� Ž���� ��θ� �޾ƿ� pathData�� currentPath �ʱ�ȭ
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

    // targetPosition���� ���ϴ� ��θ� ����ϰ�, ��ΰ� �ִٸ� ���ο� pathData�� currentPath�� ������
    private bool CalculateAndSetPath(Vector3 currentPosition, Vector3 targetPosition)
    {
        List<PathNode>? tempPathNodes = PathfindingManager.Instance!.FindPathAsNodes(currentPosition, targetPosition);

        if (tempPathNodes == null || tempPathNodes.Count == 0) return false; // �������� ���ϴ� ��ΰ� ����

        SetNewPath(tempPathNodes);
        return true;
    }

    // ���� ��ġ���� ���� ����� �ٸ����̵带 �����ϰ�, �ٸ����̵�� ���ϴ� ��θ� ������
    private void SetBarricadePath()
    {
        targetBarricade = PathfindingManager.Instance!.GetNearestBarricade(transform.position);

        if (targetBarricade != null)
        {
            CalculateAndSetPath(transform.position, targetBarricade.transform.position);
        }
    }

    // �������� ���ϴ� ��θ� ã��, ���ٸ� ���� ����� �ٸ����̵�� ���ϴ� ��θ� ������
    private void FindPathToDestinationOrBarricade()
    {
        if (!CalculateAndSetPath(transform.position, destinationPosition))
        {
            Debug.Log("�������� ���ϴ� ��� �߰� �� ����");
            SetBarricadePath();
        }
    }


    // �������̽� ������ ����
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


    // ���� ��� �ð�, ���� ��Ÿ�� �ð� ����
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
        // �ٸ� ��ü�� �ٸ� �±׸� ������ ��
        string id = GetInstanceID().ToString();

        // ���� ���� ����Ʈ Ǯ ����
        if (BaseData.meleeAttackEffectPrefab != null)
        {
            meleeAttackEffectTag = id + BaseData.entityName + BaseData.meleeAttackEffectPrefab.name;
            ObjectPoolManager.Instance!.CreatePool(
                meleeAttackEffectTag,
                BaseData.meleeAttackEffectPrefab
            );
        }

        // �ǰ� ����Ʈ Ǯ ����
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
        // ����Ʈ ó��
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
        float actualDamage = 0; // �Ҵ���� �ʼ�

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

        return Mathf.Max(actualDamage, 0.05f * incomingDamage); // ���� ������� 5%�� ���Բ� ����
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

    // enemy�� �ݶ��̴��� �� ��Ȳ�� ���� �� �ϴ�
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
            
            // �ٽ� : LookRoation�� +z ������ �ٶ󺸰� �����
            // forward : �ٶ� ���� / up : �� ����
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            modelContainer.transform.rotation = targetRotation;

            // �ٸ� ���
            //direction.Normalize();
            //float angle = Vector3.SignedAngle(modelContainer.transform.forward, direction, Vector3.up);
            //modelContainer.transform.eulerAngles = new Vector3(0, angle, 0);

            // ���� �ε巯�� ȸ���� ���Ѵٸ�
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

