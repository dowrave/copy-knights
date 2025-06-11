using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways] // 에디터, 런타임 모두에서 스크립트 실행
public class Tile : MonoBehaviour
{
    [Header("Highlight References")]
    [SerializeField] private Material highlightMaterial = default!;
    [SerializeField] private MeshRenderer attackRangeIndicator = default!;

    public TileData data = default!;
    public DeployableUnitEntity? OccupyingDeployable { get; private set; }
    public bool IsOccupied
    {
        get { return OccupyingDeployable != null; }
    }

    public bool IsWalkable { get; private set; }
    private float tileScale = 0.98f;
    public Vector2 size2D;

    // 이 타일을 공격 범위로 삼는 오퍼레이터 목록
    private readonly List<Operator> listeningOperators = new List<Operator>();

    // 타일 위에 있는 적들을 저장하는 리스트, 오퍼레이터의 공격 범위가 타일이므로 유지함
    private List<Enemy> enemiesOnTile = new List<Enemy>();
    public IReadOnlyList<Enemy> EnemiesOnTile => enemiesOnTile;

    /* 
     * 중요! 프로퍼티만 설정하면 변수 저장은 불가능하다
     * 필드와 프로퍼티를 함께 설정하고, 필드를 저장해야 프리팹 내부에 그리드 좌표를 저장할 수 있게 된다.
     * 즉, 아래처럼 설정하는 건 각 타일이 스스로 gridPosition 정보를 갖게 하기 위함이다.
     * public Vector2Int GridPosition {get; set;} 만 설정하면, 프리팹을 저장했다가 불러올 때 각 타일의 그리드 좌표가 날아간다.
    */
    [HideInInspector]
    [SerializeField] private Vector2Int gridPosition; 
    public Vector2Int GridPosition
    {
        get { return gridPosition; }
        private set { gridPosition = value; }
    }

    private MeshRenderer meshRenderer = default!;
    private MaterialPropertyBlock propBlock = default!; // 머티리얼 속성을 오버라이드하는 경량 객체. 모든 타일이 동일한 머티리얼을 공유하되 색을 개별적으로 설정할 수 있다.
    private Material[] originalMaterials;
    private Material[] highlightMaterials;

    // attackRange의 색깔들
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
    }

    // 자식 모델의 Mesh Renderer에 들어가는 머티리얼 배열을 2가지로 준비함
    // 1. 기본 머티리얼만 들어간 상태
    // 2. 기본 머티리얼 + 하이라이트 머티리얼이 들어간 상태

    private void PrepareHighlight()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();

        originalMaterials = meshRenderer.sharedMaterials;

        // 하이라이트 머티리얼 배열 준비
        highlightMaterials = new Material[originalMaterials.Length + 1];

        for (int i = 0; i < originalMaterials.Length; i++)
        {
            highlightMaterials[i] = originalMaterials[i];
        }

        highlightMaterials[highlightMaterials.Length - 1] = highlightMaterial;
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
        propBlock = new MaterialPropertyBlock();
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
            data.isDeployable; // 이 타일이 배치 가능한지
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
        // 색 설정
        if (isMedic)
        {
            attackRangeIndicator.material.color = medicIndicatorColor;
        }
        else
        {
            attackRangeIndicator.material.color = defaultIndicatorColor;
        }


        attackRangeIndicator.gameObject.SetActive(true);
    }

    public void HideAttackRange()
    {
        attackRangeIndicator.gameObject.SetActive(false);
    }

    public void Highlight()
    {
        meshRenderer.sharedMaterials = highlightMaterials;
    }


    public void ResetHighlight()
    {
        meshRenderer.sharedMaterials = originalMaterials;
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

    // 타일 위의 모든 적 반환
    public List<Enemy> GetEnemiesOnTile()
    {
        return enemiesOnTile;
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

    /// <summary>
    /// 바리케이드가 있는지 여부 판정
    /// </summary>
    public bool HasBarricade()
    {
        if (OccupyingDeployable is Barricade)
        {
            return true;
        }
        return false;
    }

}
