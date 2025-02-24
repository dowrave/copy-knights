using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteAlways] // ������, ��Ÿ�� ��ο��� ��ũ��Ʈ ����
public class Tile : MonoBehaviour
{
    public TileData data;
    public DeployableUnitEntity OccupyingDeployable { get; private set; }
    public bool IsOccupied
    {
        get { return OccupyingDeployable != null; }
    }

    public bool IsWalkable { get; private set; }

    private Transform cubeTransform;
    private float tileScale = 0.98f;
    public Vector2 size2D;

    // Ÿ�� ���� �ִ� ������ �����ϴ� ����Ʈ, ���۷������� ���� ������ Ÿ���̹Ƿ� ������
    private List<Enemy> enemiesOnTile = new List<Enemy>();
    public IReadOnlyList<Enemy> EnemiesOnTile => enemiesOnTile;

    /* 
     * �߿�! ������Ƽ�� �����ϸ� ���� ������ �Ұ����ϴ�
     * �ʵ�� ������Ƽ�� �Բ� �����ϰ�, �ʵ带 �����ؾ� ������ ���ο� �׸��� ��ǥ�� ������ �� �ְ� �ȴ�.
     * ��, �Ʒ�ó�� �����ϴ� �� �� Ÿ���� ������ gridPosition ������ ���� �ϱ� �����̴�.
     * public Vector2Int GridPosition {get; set;} �� �����ϸ�, �������� �����ߴٰ� �ҷ��� �� �� Ÿ���� �׸��� ��ǥ�� ���ư���.
    */
    [SerializeField] private Vector2Int gridPosition; 
    public Vector2Int GridPosition
    {
        get { return gridPosition; }
        private set { gridPosition = value; }
    }

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
        
        InitializeGridPosition();
        size2D = new Vector2(tileScale, tileScale);
    }

    private void OnValidate()
    {
        Initialize();
        InitializeGridPosition();
    }

    // ������Ʈ Ȱ��ȭ���� ȣ��
    private void OnEnable()
    {
        Initialize();
        InitializeGridPosition();

    }

    private void Initialize()
    {
        cubeTransform = transform.Find("Cube");

        // �ڽ� ������Ʈ Cube�� Renderer�� �����´�.
        if (tileRenderer == null)
        {
            tileRenderer = cubeTransform.GetComponentInChildren<Renderer>();
        }

        tileRenderer.sharedMaterial = baseTileMaterial;

        propBlock = new MaterialPropertyBlock();
        UpdateVisuals();
    }

    public void SetTileData(TileData tileData, Vector2Int gridPosition)
    {
        data = tileData;
        GridPosition = gridPosition;
        IsWalkable = data.isWalkable;

        AdjustScale();
        UpdateVisuals();
    }

    public void AdjustScale()
    {
        Vector3 targetScale = new Vector3(tileScale, GetHeightScale(), tileScale);
        transform.localScale = targetScale;
    }

    // ��ġ�� ��Ҵ� �� ���� ���ݺ��� ���� ���� ��
    public float GetHeightScale()
    {
        return (data != null && data.terrain == TileData.TerrainType.Hill) ? 0.5f : 0.1f;
    }

    protected void UpdateVisuals()
    {
        if (tileRenderer == null || data == null) return;

        tileRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_BaseColor", data.tileColor);
        tileRenderer.SetPropertyBlock(propBlock);
    }

    public bool CanPlaceDeployable()
    {
        return 
            !data.isStartPoint && // ������ �ƴ�
            !data.isEndPoint && // ���� �ƴ�
            (OccupyingDeployable == null) &&  // �����ϰ� �ִ� ��ü ����
            (data.isDeployable); // �� Ÿ���� ��ġ ��������
    }

    public void SetOccupied(DeployableUnitEntity deployable)
    {
        OccupyingDeployable = deployable;
    }


    public void ClearOccupied()
    {
        OccupyingDeployable = null;
    }

    public void Highlight(Color color)
    {
        if (tileRenderer != null)
        {
            tileRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", color);
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

    // �� Ÿ��
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
        Debug.LogWarning($"Ÿ�� �̸����� �׸��� ��ǥ ���� ����: {tileName}");
        return Vector2Int.zero;
    }

    // Ÿ�Ͽ� �ö� �� �����ϴ� �޼���� -------
    public bool IsEnemyOnTile(Enemy enemy)
    {
        Vector3 enemyPosition = enemy.transform.position;
        Vector3 tilePosition = transform.position;

        // 3D -> 2D ��ǥ�� ��ȯ
        Vector2 enemyPosition2D = new Vector2(enemyPosition.x, enemyPosition.z);
        Vector2 tilePosition2D = new Vector2(tilePosition.x, tilePosition.z);

        // Ÿ�� ��� ���
        float tolerance = 0f; // Ÿ�� ���� ������ ������ ����
        float minX = tilePosition2D.x - size2D.x / 2 - tolerance;
        float maxX = tilePosition2D.x + size2D.x / 2 + tolerance;
        float minY = tilePosition2D.y - size2D.y / 2 - tolerance;
        float maxY = tilePosition2D.y + size2D.y / 2 + tolerance;

        // ���� x, z ��ǥ�� Ÿ�� ��� ���� �ִ��� Ȯ��
        return enemyPosition2D.x >= minX && enemyPosition2D.x <= maxX && enemyPosition2D.y >= minY && enemyPosition2D.y <= maxY;
    }

    // Ÿ�� ���� ��� �� ��ȯ
    public List<Enemy> GetEnemiesOnTile()
    {
        return enemiesOnTile;
    }

    // ���� Ÿ�Ͽ� ����
    public void EnemyEntered(Enemy enemy)
    {
        if (!enemiesOnTile.Contains(enemy))
        {
            enemiesOnTile.Add(enemy);
        }
    }

    // ���� Ÿ�Ͽ��� ����
    public void EnemyExited(Enemy enemy)
    {
        enemiesOnTile.Remove(enemy);
    }


    // Ÿ�Ͽ� �ö� �� �����ϴ� �޼���� �� -------
    public void ToggleWalkable(bool isWalkable)
    {
        IsWalkable = isWalkable;
    }

    /// <summary>
    /// �ٸ����̵尡 �ִ��� ���� ����
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
