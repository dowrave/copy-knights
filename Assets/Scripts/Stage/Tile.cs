using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways] // 에디터, 런타임 모두에서 스크립트 실행
public class Tile : MonoBehaviour
{
    public TileData data;
    public Operator OccupyingOperator { get; private set; }

    private Transform cubeTransform;
    float tileScale = 0.98f;
    public Vector2 size2D;

    // 타일 위에 있는 적들을 저장하는 리스트
    public List<Enemy> enemiesOnTile = new List<Enemy>();

    /* 
     * 중요! 프로퍼티만 설정하면 변수 저장은 불가능하다
     * 필드와 프로퍼티를 함께 설정하고, 필드를 저장해야 프리팹 내부에 그리드 좌표를 저장할 수 있게 된다.
     * 즉, 아래처럼 설정하는 건 각 타일이 스스로 gridPosition 정보를 갖게 하기 위함이다.
     * public Vector2Int GridPosition {get; set;} 만 설정하면, 프리팹을 저장했다가 불러올 때 각 타일의 그리드 좌표가 날아간다.
    */
    [SerializeField] private Vector2Int gridPosition; 
    public Vector2Int GridPosition
    {
        get { return gridPosition; }
        set { gridPosition = value; }
    }

    [SerializeField] private Material baseTileMaterial; // Inspector에서 할당함
    private Renderer tileRenderer;
    private MaterialPropertyBlock propBlock; // 머티리얼 속성을 오버라이드하는 경량 객체. 모든 타일이 동일한 머티리얼을 공유하되 색을 개별적으로 설정할 수 있다.

    // 길찾기 알고리즘을 위한 속성들
    public int GCost { get; set; }
    public int HCost { get; set; }
    public int FCost => GCost + HCost;
    public Tile Parent { get; set; }

    private void Awake()
    {
        cubeTransform = transform.Find("Cube");
        InitializeGridPosition();
        size2D = new Vector2(tileScale, tileScale);
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
        // 자식 오브젝트 Cube의 Renderer를 가져온다.
        if (tileRenderer == null)
        {
            tileRenderer = GetComponentInChildren<Renderer>();
        }

        else
        {
            tileRenderer.sharedMaterial = baseTileMaterial;
        }

        propBlock = new MaterialPropertyBlock();
        UpdateVisuals();
    }

    public void SetTileData(TileData tileData, Vector2Int gridPosition)
    {
        data = tileData;
        GridPosition = gridPosition;
        AdjustCubeScale();
        UpdateVisuals();
    }

    public void AdjustCubeScale()
    {
        if (cubeTransform != null)
        {

            cubeTransform.localScale = new Vector3(tileScale, GetHeightScale(), tileScale);

            // BoxCollider 크기 조정
            BoxCollider boxCollider = cubeTransform.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.size = new Vector3(1f / tileScale, 1f / GetHeightScale(), 1f / tileScale); // 부모 오브젝트의 스케일 변경을 대비
            }

        }
    }
    private float GetHeightScale()
    {
        return (data != null && data.terrain == TileData.TerrainType.Hill) ? 0.5f : 0.1f;
    }

    public float GetHeight()
    {
        return transform.localScale.y;
    }

    protected void UpdateVisuals()
    {
        if (tileRenderer == null || data == null) return;

        tileRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", data.tileColor);
        tileRenderer.SetPropertyBlock(propBlock);
        
    }


    public bool CanPlaceOperator()
    {
        return (OccupyingOperator == null) && (data.canPlaceOperator);
    }

    public void SetOccupied(Operator op)
    {
        OccupyingOperator = op;
    }


    public void ClearOccupied()
    {
        OccupyingOperator = null;
    }

    public void Highlight(Color color)
    {
        if (tileRenderer != null)
        {
            tileRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", color);
            tileRenderer.SetPropertyBlock(propBlock);
        } 
    }

    public void ResetHighlight()
    {
        if (tileRenderer != null)
        {
            UpdateVisuals();
        }
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
        UpdateEnemiesOnTile();
        return new List<Enemy>(enemiesOnTile);
    }

    // 타일 위의 적 목록 업데이트
    private void UpdateEnemiesOnTile()
    {
        enemiesOnTile.Clear();
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in allEnemies)
        {
            if (IsEnemyOnTile(enemy))
            {
                enemiesOnTile.Add(enemy);
            }
        }
    }

    // 적이 타일에 진입
    public void EnemyEntered(Enemy enemy)
    {
        if (!enemiesOnTile.Contains(enemy))
        {
            enemiesOnTile.Add(enemy);
        }
    }

    // 적이 타일에서 나감
    public void EnemyExited(Enemy enemy)
    {
        enemiesOnTile.Remove(enemy);
    }


    // 타일에 올라간 적 관리하는 메서드들 끝 -------

}
