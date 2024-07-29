using UnityEngine;

[ExecuteAlways] // 에디터, 런타임 모두에서 스크립트 실행
public class Tile : MonoBehaviour
{
    public TileData data;
    public bool IsOccupied { get; private set; }
    private Transform cubeTransform;

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
        //UpdateName();
    }

    public void AdjustCubeScale()
    {
        if (cubeTransform != null)
        {
            float tileScale = 0.98f;
            cubeTransform.localScale = new Vector3(tileScale, GetHeightScale(), tileScale);

            // BoxCollider 크기 조정
            BoxCollider boxCollider = cubeTransform.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.size = new Vector3(1f / tileScale, 1f / GetHeightScale(), 1f / tileScale); // 부모 오브젝트의 스케일 변경을 대비
            }
            //if (data == null) return;
            //float height = (data.terrain == TileData.TerrainType.Hill) ? 0.5f : 0.1f;
            //transform.localScale = new Vector3(transform.localScale.x , height, transform.localScale.z);
            //transform.localPosition = new Vector3(transform.localPosition.x, height / 2f, transform.localPosition.z);

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

    private void UpdateVisuals()
    {
        if (tileRenderer == null || data == null) return;

        tileRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", data.tileColor);
        tileRenderer.SetPropertyBlock(propBlock);
        
    }


    public bool CanPlaceOperator()
    {
        return (IsOccupied == false) && (data.canPlaceOperator);
    }

    public void SetOccupied(bool occupied)
    {
        IsOccupied = occupied;
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

    //private void UpdateName()
    //{
    //    string tileName = $"Tile_{GridPosition.x}_{GridPosition.y}";
    //    if (data.isStartPoint)
    //    {
    //        tileName += "_Start";
    //    }
    //    else if (data.isEndPoint)
    //    {
    //        tileName += "_End";
    //    }
    //    gameObject.name = tileName;
    //}
}
