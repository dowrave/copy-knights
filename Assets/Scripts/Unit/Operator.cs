using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class Operator : Unit, IClickable
{

    [SerializeField] // 필드 직렬화, Inspector에서 이 필드 숨기기
    public OperatorData data;

    [SerializeField, HideInInspector]
    public Vector3 facingDirection = Vector3.left;

    // 저지 관련
    private List<Enemy> blockedEnemies; // 내가 저지 중인 적들
    public IReadOnlyList<Enemy> BlockedEnemies => blockedEnemies.AsReadOnly();

    public int deploymentOrder { get; private set; } // 배치 순서
    private bool isDeployed = false; // 배치 완료 시 true
    private Map currentMap;
    private Enemy currentTarget;
    private float attackCooldown = 0f; // data.baseStats에서 들어오는 AttackSpeed 값에 의해 결정됨
    //[HideInInspector] public bool isBlocking = false; // 저지 중인가

    // SP 관련
    public float currentHealth => stats.Health;
    // 최대 체력
    private float maxHealth;
    public float MaxHealth => maxHealth;
    private float currentSP;
    public float CurrentSP => currentSP;

    [SerializeField] private GameObject operatorUIPrefab;
    private OperatorUI operatorUI;

    // 공격 범위 내에 있는 적들 
    List<Enemy> enemiesInRange = new List<Enemy>();

    // 미리보기 관련
    private bool isPreviewMode = false;
    public bool IsPreviewMode
    {
        get { return isPreviewMode; }
        set
        {
            isPreviewMode = value;
            UpdateVisuals();
        }
    }
    private MeshRenderer meshRenderer;
    private Material originalMaterial;
    private Material previewMaterial;

    private SpriteRenderer directionIndicator;

    // 필드 끝 --------------------------------------------------------

    private void Awake()
    {
        PrepareTransparentMaterial();
        currentMap = FindObjectOfType<Map>();
    }

    // 배치에 상관 없는 초기화 수행
    private void Start()
    {
        currentSP = data.initialSP; // SP 초기화
        attackRangeType = data.attackRangeType;
        CreateDirectionIndicator();
        InitializeStats();
    }

    public void Deploy(Vector3 position, Vector3 direction)
    {
        if (!isDeployed)
        {
            isDeployed = true;
            transform.position = position;
            SetDirection(direction);

            maxHealth = data.baseStats.Health;
            //currentMap = FindObjectOfType<Map>();

            CreateOperatorUI();
        }
    }

    public void SetDirection(Vector3 direction)
    {
        facingDirection = direction.normalized;
        transform.forward = facingDirection;
        UpdateDirectionIndicator(facingDirection);
    }

    private void CreateOperatorUI()
    {
        if (operatorUIPrefab != null)
        {
            GameObject uiObject = Instantiate(operatorUIPrefab, transform);
            operatorUI = uiObject.GetComponentInChildren<OperatorUI>();
            operatorUI.Initialize(this);
        }
    }

    public void InitializeStats()
    {
        base.Initialize(data.baseStats);
        attackRangeType = data.attackRangeType;
        blockedEnemies = new List<Enemy>(data.maxBlockableEnemies);
    }


    private void Update()
    {
        if (isDeployed)
        {
            attackCooldown -= Time.deltaTime;



            // 공격 대상
            if (currentTarget == null)
            {
                FindTarget(); // currentTarget 업데이트 시도
            }

            if (attackCooldown <= 0 && currentTarget != null)
            {
                Attack(currentTarget);
            }

            RecoverSP();
        }


    }

    // 적에게 공격 중인 오퍼레이터를 알림
    private void SetAndNotifyTarget(Enemy newTarget)
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

    public override void Attack(Unit target)
    {
        if (!canAttack || !(target is Enemy enemy)) return;

        switch (attackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(enemy);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(enemy);
                break;
        }

        // 공격 후 쿨다운 설정
        attackCooldown = 1f / stats.AttackSpeed;
    }

    private void PerformMeleeAttack(Enemy enemy)
    {
        enemy.TakeDamage(stats.AttackPower);
    }

    private void PerformRangedAttack(Enemy enemy)
    {
        if (data.projectilePrefab != null)
        {

            // 투사체 생성 위치
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
            
            GameObject projectileObj = Instantiate(data.projectilePrefab, spawnPosition, Quaternion.identity);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(enemy, stats.AttackPower);
            }
        }
    }


    // 공격 타일에 있는 적들을 반환함
    private void GetEnemiesInAttackRange()
    {
        enemiesInRange.Clear();
        Vector2Int operatorGridPos = currentMap.WorldToGridPosition(transform.position);

        foreach (Vector2Int offset in data.attackableTiles)
        {
            Vector2Int rotatedOffset = RotateOffset(offset, facingDirection);
            Vector2Int targetGridPos = operatorGridPos + rotatedOffset;

            Tile targetTile = currentMap.GetTile(targetGridPos.x, targetGridPos.y);
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

    public override bool CanAttack(Vector3 targetPosition)
    {
        Vector2Int relativePosition = WorldToRelativeGridPosition(targetPosition);
        return System.Array.Exists(data.attackableTiles, tile => tile == relativePosition);
    }

    private Vector2Int WorldToRelativeGridPosition(Vector3 worldPosition)
    {
        if (currentMap != null)
        {
            Vector2Int absoluteGridPos = currentMap.WorldToGridPosition(worldPosition);
            Vector2Int operatorGridPos = currentMap.WorldToGridPosition(transform.position);
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
        return blockedEnemies.Count < data.maxBlockableEnemies;
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
        if (isDeployed == false) { return;  }

        float oldSP = currentSP;
        if (data.autoRecoverSP)
        {
            currentSP = Mathf.Min(currentSP + data.SpRecoveryRate * Time.deltaTime, data.maxSP);    

        }

        if (currentSP != oldSP)
        {
            operatorUI.UpdateSPBar(currentSP, data.maxSP);
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

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        //operatorUI.UpdateOperatorUI(this);
        operatorUI.UpdateUI();
    }

    protected override void Die()
    {
        // 사망 후 작동해야 하는 로직이 있을 듯?
        UnblockAllEnemies();

        // 오브젝트 파괴
        Destroy(operatorUI.gameObject); // 하단 체력/SP 바
        Destroy(directionIndicator.gameObject); // 방향 표시기
        base.Die();

        // 하단 UI 활성화
        OperatorManager.Instance.OnOperatorRemoved(data);
    }

    private void PrepareTransparentMaterial()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
            previewMaterial = new Material(originalMaterial);
            previewMaterial.SetFloat("_Mode", 3); // TransParent 모드로 설정
            Color previewColor = previewMaterial.color;
            previewColor.a = 0.5f;
            previewMaterial.color = previewColor;
        }
    }

    private void UpdateVisuals()
    {
        if (isPreviewMode)
        {
            // 프리뷰 모드일 때의 시각 설정
            meshRenderer.material = previewMaterial;
        }
        else
        {
            // 실제 배치 모드일 때의 시각 설정
            meshRenderer.material = originalMaterial;

        }
    }

    public void ShowActionUI()
    {
        OperatorManager.Instance.ShowActionUI(this);
    }

    public void UseSkill()
    {
        // 스킬 사용 로직
        Debug.Log("스킬 버튼 클릭됨");
    }

    public void Retreat()
    {
        Debug.Log("퇴각 버튼 클릭됨");

        // 수정 필요) 사망 vs 퇴각의 차이가 필요 - 퇴각은 반환 배치 코스트가 있다
        OperatorManager.Instance.OnOperatorRemoved(data);

        Destroy(gameObject);

    }

    /// <summary>
    /// 오퍼레이터가 클릭되었을 때의 동작 
    /// </summary>
    public void OnClick()
    {
        if (isDeployed && !IsPreviewMode && StageManager.Instance.currentState == GameState.Battle)
        {
            OperatorManager.Instance.CancelPlacement(); // 오퍼레이터를 클릭했다면 현재 진행 중인 배치 로직이 취소되어야 함

            // 미리보기 상태에선 동작하면 안됨
            if (IsPreviewMode == false)
            {
                UIManager.Instance.ShowOperatorInfo(data, transform.position);
            }

            HighlightAttackRange();
            ShowActionUI();
        }
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
        if (currentMap == null) return;

        Vector2Int operatorGridPos = currentMap.WorldToGridPosition(transform.position);
        List<Tile> tilesToHighlight = new List<Tile>();

        foreach (Vector2Int offset in data.attackableTiles)
        {
            Vector2Int rotatedIOffset = RotateOffset(offset, facingDirection);
            Vector2Int targetGridPos = operatorGridPos + rotatedIOffset;
            Tile targetTile = currentMap.GetTile(targetGridPos.x, targetGridPos.y);
            if (targetTile != null)
            {
                tilesToHighlight.Add(targetTile);
            }
        }

        OperatorManager.Instance.HighlightTiles(tilesToHighlight, OperatorManager.Instance.attackRangeTileColor);
    }
}
