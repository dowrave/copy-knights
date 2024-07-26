using UnityEngine;

[ExecuteAlways] // ������, ��Ÿ�� ��ο��� ��ũ��Ʈ ����
public class Tile : MonoBehaviour
{
    public TileData data;
    public Vector2Int GridPosition { get; private set; }
    //public Vector2Int GridPosition;
    public bool IsOccupied { get; private set; }
    private Transform cubeTransform;

    // spawner ���� ����
    //public bool isSpawnPoint; // Start Ÿ�ϸ� üũ�ϸ� �ǹǷ�
    //public EnemySpawner spawner; // EnemySpawner ������Ʈ�� ���� ����

    [SerializeField] private Material baseTileMaterial; // Inspector���� �Ҵ���
    private Renderer tileRenderer;
    private MaterialPropertyBlock propBlock; // ��Ƽ���� �Ӽ��� �������̵��ϴ� �淮 ��ü. ��� Ÿ���� ������ ��Ƽ������ �����ϵ� ���� ���������� ������ �� �ִ�.

    // ��ã�� �˰����� ���� �Ӽ���
    public int GCost { get; set; }
    public int HCost { get; set; }
    public int FCost => GCost + HCost;
    public Tile Parent { get; set; }

    private void Awake()
    {
        cubeTransform = transform.Find("Cube");
    }

    // ������Ʈ Ȱ��ȭ���� ȣ��
    private void OnEnable()
    {
        Initialize();
    }

    // �ν����Ϳ��� ������Ʈ ���� ����� ������ ȣ�� - �����Ϳ��� �ٷ� Ȯ�� ����
    private void OnValidate()
    {
        Initialize();
    }

    private void Initialize()
    {
        // �ڽ� ������Ʈ Cube�� Renderer�� �����´�.
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

            // BoxCollider ũ�� ����
            BoxCollider boxCollider = cubeTransform.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.size = new Vector3(1f / tileScale, 1f / GetHeightScale(), 1f / tileScale); // �θ� ������Ʈ�� ������ ������ ���
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
