using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// [ExecuteAlways] // 에디터, 런타임 모두에서 스크립트 실행
public class Tile : MonoBehaviour
{
    [Header("Highlight References")]
    [SerializeField] private MeshRenderer attackRangeIndicator = default!;

    public TileData data = default!;
    public DeployableUnitEntity? OccupyingDeployable { get; private set; }

    // 이 타일을 공격 범위로 삼는 오퍼레이터 목록
    private readonly List<Operator> listeningOperators = new List<Operator>();

    public bool IsWalkable { get; private set; }
    private float tileScale = 0.98f;
    public Vector2 size2D;

    // 타일 위의 적들을 관리하는 해쉬셋.
    private HashSet<Enemy> enemiesOnTile = new HashSet<Enemy>();
    public IReadOnlyCollection<Enemy> EnemiesOnTile => enemiesOnTile;

    /* 
     * 중요! 프로퍼티만 설정하면 변수 저장은 불가능하다
     * 필드와 프로퍼티를 함께 설정하고, 필드를 저장해야 프리팹 내부에 그리드 좌표를 저장할 수 있게 된다.
     * 즉, 아래처럼 설정하는 건 각 타일이 스스로 gridPosition 정보를 갖게 하기 위함이다.
     * public Vector2Int GridPosition {get; set;} 만 설정하면, 프리팹을 저장했다가 불러올 때 각 타일의 그리드 좌표가 날아간다.
    */
    [HideInInspector][SerializeField] private Vector2Int gridPosition;
    public Vector2Int GridPosition
    {
        get { return gridPosition; }
        private set { gridPosition = value; }
    }

    // 색상 설정을 위한 MaterialPropertyBlock
    private MeshRenderer meshRenderer = default!;
    private MaterialPropertyBlock propBlock = default!; // 머티리얼 속성을 오버라이드하는 경량 객체. 모든 타일이 동일한 머티리얼을 공유하되 색을 개별적으로 설정할 수 있다.
    private MaterialPropertyBlock indicatorPropBlock = default!;

    // 타일에 나타나는 UI 색깔들
    private Color tileHighlightColor = new Color(0f, 0.25f, 0f);
    private Color defaultIndicatorColor = new Color(0.94f, 0.56f, 0.12f);
    private Color medicIndicatorColor = new Color(0.12f, 0.65f, 0.95f);

    // 길찾기 알고리즘을 위한 속성들
    public int GCost { get; set; }
    public int HCost { get; set; }
    public int FCost => GCost + HCost;
    public Tile Parent { get; set; } = default!;

    private void Awake()
    {
        PrepareHighlight();
        InitializeGridPosition();
        size2D = new Vector2(tileScale, tileScale);

        Enemy.OnEnemyDespawned += HandleEnemyDespawn;
    }

    private void PrepareHighlight()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        propBlock = new MaterialPropertyBlock();
        indicatorPropBlock = new MaterialPropertyBlock(); // 초기화

        ResetHighlight();
    }

    private void OnValidate()
    {
        Initialize();
        InitializeGridPosition();
    }

    // 오브젝트 활성화마다 호출
    private void OnEnable()
    {
        Initialize();
        InitializeGridPosition();

    }

    private void Initialize()
    {
        InitializeVisuals();
        InitializeIndicatorPosition();
    }

    private void InitializeIndicatorPosition()
    {
        // 타일 월드 위치를 기준으로 y값은 타일의 y스케일의 절반 + 0.01
        Vector3 tilePosition = transform.position;
        float indicatorY = transform.localScale.y / 2f + 0.01f;
        attackRangeIndicator.gameObject.transform.position = new Vector3(tilePosition.x, indicatorY, tilePosition.z);

        // 최초에는 비활성화
        attackRangeIndicator.gameObject.SetActive(false);
    }

    public void SetTileData(TileData tileData, Vector2Int gridPosition)
    {
        data = tileData;
        GridPosition = gridPosition;
        IsWalkable = data.isWalkable;

        AdjustScale();
        InitializeVisuals();
    }

    public void AdjustScale()
    {
        Vector3 targetScale = new Vector3(tileScale, GetHeightScale(), tileScale);
        transform.localScale = targetScale;
    }

    // 배치될 요소는 이 값의 절반보다 위에 놔야 함
    public float GetHeightScale()
    {
        return (data != null && data.terrain == TileData.TerrainType.Hill) ? 0.5f : 0.1f;
    }

    private void InitializeVisuals()
    {
        if (meshRenderer == null || data == null) return;

        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_BaseColor", data.tileColor);
        meshRenderer.SetPropertyBlock(propBlock);
    }

    public bool CanPlaceDeployable()
    {
        return
            !data.isStartPoint && // 시작점 아님
            !data.isEndPoint && // 끝점 아님
            (OccupyingDeployable == null) &&  // 차지하고 있는 객체 없음
            (data.isDeployable); // 이 타일이 배치 가능한지
    }

    public void SetOccupied(DeployableUnitEntity deployable)
    {
        OccupyingDeployable = deployable;
    }


    public void ClearOccupied()
    {
        OccupyingDeployable = null;
    }

    public void ShowAttackRange(bool isMedic)
    {
        Color targetColor = isMedic ? medicIndicatorColor : defaultIndicatorColor;

        attackRangeIndicator.GetPropertyBlock(indicatorPropBlock);
        indicatorPropBlock.SetColor("_Color", targetColor); // URP Lit과 달리, attackRange라는 별도의 셰이더가 있고 Color라는 프로퍼티가 있음
        attackRangeIndicator.SetPropertyBlock(indicatorPropBlock);

        attackRangeIndicator.gameObject.SetActive(true);
    }

    public void HideAttackRange()
    {
        attackRangeIndicator.gameObject.SetActive(false);
    }

    public void Highlight()
    {
        // materialInstance.EnableKeyword("_EMISSION");

        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_EmissionColor", tileHighlightColor);
        meshRenderer.SetPropertyBlock(propBlock);
    }

    public void ResetHighlight()
    {
        // materialInstance.DisableKeyword("_EMISSION");

        meshRenderer.GetPropertyBlock(propBlock);
        // _EmissionColor 속성을 검은색으로 설정하여 효과를 끕니다.
        propBlock.SetColor("_EmissionColor", Color.black);
        meshRenderer.SetPropertyBlock(propBlock);
    }

    public EnemySpawner GetSpawner()
    {
        return GetComponentInChildren<EnemySpawner>();
    }

    public bool HasSpawner()
    {
        return GetSpawner() != null;
    }

    public void SetGridPosition(Vector2Int gridPos)
    {
        GridPosition = gridPos;
    }

    public Vector3 GetWorldPosition()
    {
        Map map = GetComponentInParent<Map>();
        return new Vector3(GridPosition.x, 0, map.Height - 1 - GridPosition.y);
    }

    // 각 타일
    private void InitializeGridPosition()
    {
        GridPosition = ExtractGridPositionFromName(name);
    }

    private Vector2Int ExtractGridPositionFromName(string tileName)
    {
        string[] parts = tileName.Split('_');
        if (parts.Length >= 3 && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
        {
            return new Vector2Int(x, y);
        }
        Debug.LogWarning($"타일 이름에서 그리드 좌표 추출 실패: {tileName}");
        return Vector2Int.zero;
    }

    // 타일에 올라간 적 관리하는 메서드들 -------
    public bool IsEnemyOnTile(Enemy enemy)
    {
        Vector3 enemyPosition = enemy.transform.position;
        Vector3 tilePosition = transform.position;

        // 3D -> 2D 좌표로 변환
        Vector2 enemyPosition2D = new Vector2(enemyPosition.x, enemyPosition.z);
        Vector2 tilePosition2D = new Vector2(tilePosition.x, tilePosition.z);

        // 타일 경계 계산
        float tolerance = 0f; // 타일 간의 간격을 생각한 오차
        float minX = tilePosition2D.x - size2D.x / 2 - tolerance;
        float maxX = tilePosition2D.x + size2D.x / 2 + tolerance;
        float minY = tilePosition2D.y - size2D.y / 2 - tolerance;
        float maxY = tilePosition2D.y + size2D.y / 2 + tolerance;

        // 적의 x, z 좌표가 타일 경계 내에 있는지 확인
        return enemyPosition2D.x >= minX && enemyPosition2D.x <= maxX && enemyPosition2D.y >= minY && enemyPosition2D.y <= maxY;
    }

    // 적이 타일에 진입
    public void EnemyEntered(Enemy enemy)
    {
        if (!enemiesOnTile.Contains(enemy))
        {
            enemiesOnTile.Add(enemy);

            // 이 타일을 공격범위로 하는 오퍼레이터에게 알림
            foreach (var op in listeningOperators)
            {
                op.OnEnemyEnteredAttackRange(enemy);
            }
        }
    }

    // 적이 타일에서 나감
    public void EnemyExited(Enemy enemy)
    {
        enemiesOnTile.Remove(enemy);

        // 이 타일을 공격범위로 하는 오퍼레이터에게 알림
        foreach (var op in listeningOperators)
        {
            op.OnEnemyExitedAttackRange(enemy);
        }
    }

    // 타일에 올라간 적 관리하는 메서드들 끝 -------
    public void ToggleWalkable(bool isWalkable)
    {
        IsWalkable = isWalkable;
    }

    // 바리케이드가 있는지 여부 판정
    public bool HasBarricade()
    {
        if (OccupyingDeployable is Barricade)
        {
            return true;
        }
        return false;
    }

    // 오퍼레이터가 타일을 공격 범위로 등록
    public void RegisterOperator(Operator op)
    {
        if (!listeningOperators.Contains(op))
        {
            listeningOperators.Add(op);
        }
    }

    // 오퍼레이터가 타일을 공격 범위에서 해제
    public void UnregisterOperator(Operator op)
    {
        if (listeningOperators.Contains(op))
        {
            listeningOperators.Remove(op);
        }

    }

    // 적 사망 이벤트를 받아 실행
    private void HandleEnemyDespawn(Enemy enemy, DespawnReason reason)
    {
        // 타일 위에 적이 있다면 제거
        if (enemiesOnTile.Contains(enemy))
        {
            EnemyExited(enemy);
        }
    }

    void OnDestroy()
    {
        Enemy.OnEnemyDespawned -= HandleEnemyDespawn;
    }

}
