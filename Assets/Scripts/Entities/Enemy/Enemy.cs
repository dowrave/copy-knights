using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static ICombatEntity;
using System;

public enum DespawnReason
{
    Null, // ����Ʈ
    Defeated, // óġ��
    ReachedGoal // ������ ����
}

public class Enemy : UnitEntity, IMovable, ICombatEntity
{
    [SerializeField] protected EnemyData enemyData = default!;
    public virtual EnemyData BaseData => enemyData;

    protected EnemyStats currentStats;

    public override AttackType AttackType => BaseData.attackType;
    public override float AttackPower { get => currentStats.AttackPower; set => currentStats.AttackPower = value; }
    public override float AttackSpeed { get => currentStats.AttackSpeed; set => currentStats.AttackSpeed = value; }
    public AttackRangeType AttackRangeType => BaseData.attackRangeType;
    public override float MovementSpeed { get => currentStats.MovementSpeed; }
    public int BlockCount { get => BaseData.blockCount; protected set => BaseData.blockCount = value; } // Enemy�� �����ϴ� ���� ��
    public float AttackCooldown { get; set; } // ���� ���ݱ����� ��� �ð�
    public float AttackDuration { get; set; } // ���� ��� �ð�. Animator�� �߰��� �� ���� �ʿ��� ��. �׻� Cooldown���� ª�ƾ� ��.

    public float AttackRange
    {
        get
        {
            return BaseData.attackRangeType == AttackRangeType.Melee ? 0f : currentStats.AttackRange;
        }
        protected set
        {
            currentStats.AttackRange = value;
        }
    }

    public string EntityName => BaseData.entityName;

    // ��� ����
    protected PathData? pathData;
    protected List<Vector3> currentPath = new List<Vector3>();
    protected List<UnitEntity> targetsInRange = new List<UnitEntity>();
    protected PathNode nextNode = default!;
    protected int nextNodeIndex = 0; // �������ڸ��� 1�� ��
    protected Vector3 nextNodeWorldPosition; // ���� ����� ��ǥ
    protected Vector3 destinationPosition; // ������
    protected bool isWaiting = false; // �ܼ��� ��ġ���� ��ٸ��� ����
    protected bool stopAttacking = false; // ���������� ���� ���� ���� / �Ұ��� ����
    protected Barricade? targetBarricade;

    public PathNode NextNode => nextNode;
    public Vector3 NextNodeWorldPosition => nextNodeWorldPosition; 
    public Vector3 DestinationPosition => DestinationPosition; 
    

    protected Operator? blockingOperator; // �ڽ��� ���� ���� ���۷�����
    public Operator? BlockingOperator => blockingOperator;
    protected UnitEntity? _currentTarget;
    public UnitEntity? CurrentTarget
    {
        get => _currentTarget;
        protected set
        {
            // Ÿ���� ���� ���� �����ϴٸ� ���� ���� X
            if (_currentTarget == value) return;

            // ���� Ÿ���� �̺�Ʈ ���� ����
            if (_currentTarget != null)
            {
                _currentTarget.OnDeathAnimationCompleted -= OnCurrentTargetDied;
                _currentTarget.RemoveAttackingEntity(this);
            }

            _currentTarget = value;

            if (_currentTarget != null)
            {
                _currentTarget.OnDeathAnimationCompleted += OnCurrentTargetDied;
                NotifyTarget();
            }
        }
    } // ���� ���

    protected int initialPoolSize = 5;
    protected string? projectileTag;

    [SerializeField] protected GameObject enemyBarUIPrefab = default!;
    protected EnemyBarUI? enemyBarUI;

    // ���� ���� Ÿ�� ����
    protected List<Tile> contactedTiles = new List<Tile>();

    // �޽��� ȸ�� �����ؼ� �� ����
    [Header("Model Components")]
    [SerializeField] protected GameObject modelContainer = default!;

    [SerializeField] protected EnemyAttackRangeController attackRangeController = default!;

    // ICrowdControlTarget
    public Vector3 Position => transform.position;

    protected DespawnReason currentDespawnReason = DespawnReason.Null;

    protected bool isInitialized = false;

    // ����ƽ �̺�Ʈ �׽�Ʈ
    // public static event Action<Enemy> OnEnemyDestroyed; // �״� ��Ȳ + �������� �����ؼ� ������� ��Ȳ ��� ����
    public static event Action<Enemy, DespawnReason> OnEnemyDespawned = delegate { };

    protected override void Awake()
    {
        Faction = Faction.Enemy;

        InitializeModelComponents();

        // ���� ���� ��Ʈ�ѷ� �߰�
        if (attackRangeController == null)
        {
            attackRangeController = GetComponentInChildren<EnemyAttackRangeController>();
        }

        base.Awake();

        SetColliderState(true); // base.Awake���� false�� �����ǹǷ� �ٲ���

        // OnDeathAnimationCompleted += HandleDeathAnimationCompleted;
    }

    // �� ȸ�� ���� ������ �� ���� Enemy ���̶� ���⿡ �����س���.
    protected void InitializeModelComponents()
    {
        if (modelContainer == null)
        {
            modelContainer = transform.Find("ModelContainer").gameObject;
        }
    }

    public virtual void Initialize(EnemyData enemyData, PathData pathData)
    {
        this.enemyData = enemyData;
        SetPrefab();
        currentStats = enemyData.stats;
        this.pathData = pathData;

        InitializeHP();

        SetupInitialPosition();
        CreateEnemyBarUI();
        UpdateNextNode();
        InitializeCurrentPath();


        // ���� ���� �ݶ��̴� ����
        attackRangeController.Initialize(this);

        // ���ʿ� ������ ��ΰ� ���� ��Ȳ�� �� ����
        if (PathfindingManager.Instance!.IsBarricadeDeployed && IsPathBlocked())
        {
            FindPathToDestinationOrBarricade();
        }

        // ��ų ���� 
        SetSkills();

        // ������Ʈ Ǯ ����
        CreateObjectPool();

        isInitialized = true;
    }

    public override void SetPrefab()
    {
        prefab = enemyData.prefab;
    }

    protected void OnEnable()
    {
        Barricade.OnBarricadeDeployed += OnBarricadePlaced;
        Barricade.OnBarricadeRemoved += OnBarricadeRemovedWithDelay;
    }

    protected void OnDisable()
    {
        Barricade.OnBarricadeDeployed -= OnBarricadePlaced;
        Barricade.OnBarricadeRemoved -= OnBarricadeRemovedWithDelay;
    }

    protected void SetupInitialPosition()
    {
        if (pathData == null || pathData.nodes == null) return;

        if (pathData!.nodes!.Count > 0)
        {
            transform.position = MapManager.Instance!.ConvertToWorldPosition(pathData.nodes![0].gridPosition) +
                Vector3.up * BaseData.defaultYPosition;
        }
    }

    protected override void Update()
    {
        if (StageManager.Instance!.currentState == GameState.Battle && // ���� ���̸鼭
            currentDespawnReason == DespawnReason.Null && // �����ǰ� ���� ���� ��
            isInitialized
            )
        {
            // �ൿ�� �Ұ����ص� �����ؾ� �ϴ� ȿ��
            UpdateAllCooldowns();
            base.Update(); // ���� ȿ�� ����

            if (HasRestriction(ActionRestriction.CannotAction)) return;

            // �Ǵ��ϰ� �ൿ�ϴ� ������ ���� �޼���� �и�, �ڽ� Ŭ�������� ������ ������ �� �ֵ��� ��
            // �̸� ���ø� �޼��� �����̶�� �Ѵ�.
            DecideAndPerformAction();
        }
    }

    protected virtual void UpdateAllCooldowns()
    {
        UpdateAttackDuration();
        UpdateAttackCooldown();
    }

    // �ൿ ��Ģ.
    protected virtual void DecideAndPerformAction()
    {
        if (nextNodeIndex < pathData.nodes.Count)
        {
            if (AttackDuration > 0) return;  // ���� ��� ��

            // ���� ���� ���� �� ����Ʈ & ���� ���� ��� ����
            SetCurrentTarget();

            if (TryUseSkill()) return;

            // �������� - �ٰŸ� ����
            if (blockingOperator != null && CurrentTarget == blockingOperator)
            {
                if (CanAttack())
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

    // �������� ���
    protected virtual bool TryUseSkill() { return false; }

    // pathData.nodes�� �̿��� currentPath �ʱ�ȭ
    protected void InitializeCurrentPath()
    {
        if (pathData == null || pathData.nodes == null) throw new InvalidOperationException("null�� ������ ����");

        foreach (var node in pathData.nodes)
        {
            currentPath.Add(MapManager.Instance!.ConvertToWorldPosition(node.gridPosition) + Vector3.up * BaseData.defaultYPosition);
        }

        destinationPosition = currentPath[currentPath.Count - 1]; // ������ ����
    }

    // ��θ� ���� �̵�
    protected void MoveAlongPath()
    {
        if (nextNodeWorldPosition == null || destinationPosition == null) throw new InvalidOperationException("����/������ ��尡 �����Ǿ����� ����");

        if (CheckIfReachedDestination())
        {
            ReachDestination();
            return;
        }

        Move(nextNodeWorldPosition);
        RotateModelTowardsMovementDirection();

        // ��� ���� Ȯ��
        if (Vector3.Distance(transform.position, nextNodeWorldPosition) < 0.05f)
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
    protected IEnumerator WaitAtNode(float waitTime)
    {
        SetIsWaiting(true);
        yield return new WaitForSeconds(waitTime);
        SetIsWaiting(false);

        UpdateNextNode();
    }

    // ��带 �����ؾ� �ϴ� ��Ȳ�� ȣ��
    // ���� ��� �ε����� �����ϰ� ���� �������� ������
    // ��ų���� ������ �� �ְ� public���� ����
    public void UpdateNextNode()
    {
        // pathData ���� ������ �׸��� ���ų�, ������ ��尡 ������ ����� ���� ������� ����
        if (pathData == null || pathData.nodes == null || nextNodeIndex >= pathData.nodes.Count - 1) return;

        nextNodeIndex++;
        if (nextNodeIndex < pathData.nodes.Count)
        {
            nextNode = pathData.nodes[nextNodeIndex];
            nextNodeWorldPosition = MapManager.Instance!.ConvertToWorldPosition(nextNode.gridPosition) +
                Vector3.up * BaseData.defaultYPosition;
        }
    }

    public virtual void OnTargetEnteredRange(DeployableUnitEntity target)
    {
        if (target == null ||
        target.Faction != Faction.Ally || // Ally�� ������
        !target.IsDeployed || // ��ġ�� ��Ҹ� ������
        targetsInRange.Contains(target)) return; // �̹� ������ ��Ҵ� �������� ����

        targetsInRange.Add(target);
    }

    public virtual void OnTargetExitedRange(DeployableUnitEntity target)
    {
        if (targetsInRange.Contains(target))
        {
            targetsInRange.Remove(target);
        }
    }

    public void Attack(UnitEntity target, float damage)
    {
        float polishedDamage = Mathf.Floor(damage);
        PerformAttack(target, polishedDamage);
    }

    protected void PerformAttack(UnitEntity target, float damage)
    {
        AttackType finalAttackType = AttackType;
        bool showDamagePopup = false;

        foreach (var buff in activeBuffs)
        {
            buff.OnBeforeAttack(this, ref damage, ref finalAttackType, ref showDamagePopup);
        }

        switch (AttackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target, damage);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target, damage);
                break;
        }
        
        foreach (var buff in activeBuffs)
        {
            buff.OnAfterAttack(this, target);
        }
    }

    protected void PerformMeleeAttack(UnitEntity target, float damage)
    {
        SetAttackTimings(); // �̰� ���� ȣ���ϴ� ��찡 �־ ���⼭ �ٽ� ����

        AttackSource attackSource = new AttackSource(
            attacker: this,
            position: transform.position,
            damage: damage,
            type: AttackType,
            isProjectile: false,
            hitEffectPrefab: BaseData.HitEffectPrefab,
            hitEffectTag: hitEffectTag,
            showDamagePopup: false
        );

        PlayMeleeAttackEffect(target, attackSource);
        target.TakeDamage(attackSource);
    }

    protected void PerformRangedAttack(UnitEntity target, float damage)
    {
        SetAttackTimings();
        
        if (BaseData.projectilePrefab != null && projectileTag != null)
        {
            // ����ü ���� ��ġ
            Vector3 spawnPosition = transform.position;
            GameObject? projectileObj = ObjectPoolManager.Instance!.SpawnFromPool(projectileTag, spawnPosition, Quaternion.identity);

            if (projectileObj != null)
            {
                Projectile? projectile = projectileObj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(this, target, damage, false, projectileTag, BaseData.hitEffectPrefab, hitEffectTag, AttackType);
                }
            }
        }
    }

    protected bool IsTargetInRange(UnitEntity target)
    {
        return Vector3.Distance(target.transform.position, transform.position) <= AttackRange;
    }

    // ������� ���� ����
    protected void Despawn()
    {
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

        // ų ī��Ʈ / ���� ���� �ٷ� �����ϰ� ����
        OnEnemyDespawned?.Invoke(this, currentDespawnReason);

        PlayDeathAnimation(); // ���� �̺�Ʈ �߻����� ���� HandleDeathAnimationCompleted�� �����.
    }

    protected override void Die()
    {
        // �̹� ó�� ������ Ȯ��
        if (currentDespawnReason != DespawnReason.Null) return;

        // ��� �̺�Ʈ ó��
        currentDespawnReason = DespawnReason.Defeated;
        Despawn();
    }

    protected void ReachDestination()
    {
        // �̹� ó�� ������ Ȯ��
        if (currentDespawnReason != DespawnReason.Null) return;

        currentDespawnReason = DespawnReason.ReachedGoal;
        Despawn();
    }

    protected override void OnDamageTaken(UnitEntity attacker, float actualDamage)
    {
        // �����ڰ� Operator�� �� ��� �г� ������Ʈ
        if (attacker is Operator op)
        {
            OperatorData opData = op.OperatorData;
            if (opData != null)
            {
                StatisticsManager.Instance!.UpdateDamageDealt(op.OperatorData, actualDamage);
            }
        }
    }
    // ������ Ÿ���� ���� ��ǥ ����
    protected bool CheckIfReachedDestination()
    {
        if (pathData == null || pathData.nodes == null) throw new InvalidOperationException("pathData, pathData.nodes�� ����");

        if (pathData.nodes.Count == 0) return false;

        Vector2Int lastNodeGridPos = pathData!.nodes![pathData!.nodes!.Count - 1].gridPosition;
        Vector3 lastNodePosition = MapManager.Instance!.ConvertToWorldPosition(lastNodeGridPos) + Vector3.up * 0.5f;

        return Vector3.Distance(transform.position, lastNodePosition) < 0.05f;
    }

    // Enemy�� ������ ��� ����. Update���� ��� ���ư� �ʿ䰡 �ִ�.
    public void SetCurrentTarget()
    {
        // ���� Ÿ���� ���� ���� ���� ���۷����Ͷ�� ������ �ʿ� X
        if (blockingOperator != null && CurrentTarget == blockingOperator) return;

        // 1. �ڽ��� �����ϴ� ���۷����͸� Ÿ������ ����
        if (blockingOperator != null)
        {
            CurrentTarget = blockingOperator;
            return;
        }

        // 2. ���� ���� ���� Ÿ��
        if (targetsInRange.Count > 0)
        {
            // Ÿ�� ����
            UnitEntity? newTarget = targetsInRange
                .OfType<Operator>() // ���۷�����
                .OrderByDescending(o => o.DeploymentOrder) // ���� ���߿� ��ġ�� ���۷����� 
                .FirstOrDefault();

            // Ÿ���� ��ȿ�� �˻� : ���� ���� ���� �ְ� null�� �ƴ� ��
            if (newTarget != null || targetsInRange.Contains(newTarget))
            {
                CurrentTarget = newTarget;
            }

            return;
        }

        // 3. ���� �� ���ǿ� �ش����� �ʴ´ٸ� Ÿ���� �����Ѵ�.
        CurrentTarget = null;
    }

    // Enemy�� ���� ������� ���� ���� �׾��� �� ����
    public void OnCurrentTargetDied(UnitEntity destroyedTarget)
    {
        // Ÿ���� �ı��Ǿ��ٴ� �ǹ��̹Ƿ� CurrentTarget = null�θ� �������ش�.
        if (CurrentTarget == destroyedTarget)
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
                Vector3 nowPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                distance += Vector3.Distance(nowPosition, currentPath[i + 1]);
            }

            distance += Vector3.Distance(currentPath[i], currentPath[i + 1]);
        }

        return distance;
    }


    // Barricade ��ġ �� ���� ��ΰ� �����ٸ� ����
    protected void OnBarricadePlaced(Barricade barricade)
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

    protected void OnBarricadeRemovedWithDelay(Barricade barricade)
    {
        StartCoroutine(OnBarricadeRemoved(barricade));
    }

    // �ٸ����̵� ���� �� ����
    protected IEnumerator OnBarricadeRemoved(Barricade barricade)
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
    protected bool IsPathBlocked()
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
                return true;
            }
        }


        return false;
    }


    // CalculatePath�� Ž���� ��θ� �޾ƿ� pathData�� currentPath �ʱ�ȭ
    protected void SetNewPath(List<PathNode> newPathNodes)
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
    protected bool CalculateAndSetPath(Vector3 currentPosition, Vector3 targetPosition)
    {
        List<PathNode>? tempPathNodes = PathfindingManager.Instance!.FindPathAsNodes(currentPosition, targetPosition);

        if (tempPathNodes == null || tempPathNodes.Count == 0) return false; // �������� ���ϴ� ��ΰ� ����

        SetNewPath(tempPathNodes);
        return true;
    }

    // ���� ��ġ���� ���� ����� �ٸ����̵带 �����ϰ�, �ٸ����̵�� ���ϴ� ��θ� ������
    protected void SetBarricadePath()
    {
        targetBarricade = PathfindingManager.Instance!.GetNearestBarricade(transform.position);

        if (targetBarricade != null)
        {
            CalculateAndSetPath(transform.position, targetBarricade.transform.position);
        }
    }

    // �������� ���ϴ� ��θ� ã��, ���ٸ� ���� ����� �ٸ����̵�� ���ϴ� ��θ� ������
    protected void FindPathToDestinationOrBarricade()
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

    public void SetAttackDuration(float? intentionalDuration = null)
    {
        AttackDuration = AttackSpeed / 3f;
    }

    public void SetAttackCooldown(float? intentionalCooldown = null)
    {
        AttackCooldown = AttackSpeed;
    }

    public bool CanAttack()
    {
        return CurrentTarget != null &&
            AttackCooldown <= 0 &&
            AttackDuration <= 0 &&
            !stopAttacking; 
    }

    protected virtual void CreateObjectPool()
    {
        // ��ü�� �������� Ǯ�� ������
        string baseTag = BaseData.entityName;

        // ���� ���� ����Ʈ Ǯ ����
        if (BaseData.meleeAttackEffectPrefab != null)
        {
            meleeAttackEffectTag = baseTag + BaseData.meleeAttackEffectPrefab.name;
            ObjectPoolManager.Instance!.CreatePool(
                meleeAttackEffectTag,
                BaseData.meleeAttackEffectPrefab
            );
        }

        // �� Ÿ�� ����Ʈ Ǯ ����
        if (BaseData.hitEffectPrefab != null)
        {
            hitEffectTag = baseTag + BaseData.hitEffectPrefab.name;
            ObjectPoolManager.Instance!.CreatePool(
                hitEffectTag,
                BaseData.hitEffectPrefab
            );
        }

        InitializeProjectilePool();
    }

    protected void PlayMeleeAttackEffect(UnitEntity target, AttackSource attackSource)
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

    protected void CreateEnemyBarUI()
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

    protected override void InitializeHP()
    {
        MaxHealth = Mathf.Floor(currentStats.Health);
        CurrentHealth = Mathf.Floor(MaxHealth);
    }

    public void UpdateBlockingOperator(Operator? op)
    {
        blockingOperator = op;
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

    public override void OnBodyTriggerEnter(Collider other)
    {
        Tile tile = other.GetComponent<Tile>();
        if (tile != null)
        {
            contactedTiles.Add(tile);
            tile.EnemyEntered(this);
        }
    }

    public override void OnBodyTriggerExit(Collider other)
    {
        Tile tile = other.GetComponent<Tile>();
        if (tile != null && contactedTiles.Contains(tile))
        {
            tile.EnemyExited(this);
            contactedTiles.Remove(tile);
        }
    }

    public override void SetMovementSpeed(float newSpeed)
    {
        currentStats.MovementSpeed = newSpeed;
    }

    // ���� �̵� �������� ȸ����Ŵ
    // ����) ������ ���� +z �������� �̵��Ѵٰ� �������� �� �۵���
    protected void RotateModelTowardsMovementDirection()
    {
        if (modelContainer == null) return;

        Vector3 direction = nextNodeWorldPosition - transform.position;
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

    public void SetIsWaiting(bool isWaiting)
    {
        this.isWaiting = isWaiting;
    }

    public void SetStopAttacking(bool isAttacking)
    {
        this.stopAttacking = isAttacking;
    }

    protected virtual void SetSkills() { }

    protected void OnDestroy()
    {
    }

}

