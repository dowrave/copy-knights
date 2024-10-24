using UnityEngine;
using System.Collections.Generic;
using System.Text;


public class Map : MonoBehaviour
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    public int Width => width;
    public int Height => height;

    [SerializeField] private Vector3 cameraPosition;
    private Vector3 cameraRotation = new Vector3(70, 0, 0);
    public Vector3 CameraPosition => cameraPosition;
    public Vector3 CameraRotation => cameraRotation;


    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject enemySpawnerPrefab;
    [SerializeField] private TileData[] serializedTileData;
    private TileData[,] tileDataArray;
    private Dictionary<Vector2Int, GameObject> tileObjects; // ��ǥ�� Tile ������Ʈ �Ҵ�.



    // ��ũ��Ʈ Ȱ��ȭ���� �ʱ�ȭ�� Ȯ���Ѵ�
    private void OnEnable()
    {
        if (tileDataArray == null || tileObjects == null)
        {
            Initialize(width, height, true);
        }
    }

    public void Initialize(int width, int height, bool load = false)
    {
        this.width = width;
        this.height = height;
        tileDataArray = new TileData[width, height];
        tileObjects = new Dictionary<Vector2Int, GameObject>();

        if (load)
        {
            LoadExistingMap();
        }
        else
        {
            CreateNewMap();
        }
    }

    private void CreateNewMap()
    {
        tileDataArray = new TileData[width, height];
        tileObjects = new Dictionary<Vector2Int, GameObject>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // �⺻ Ÿ�� �����ͷ� �ʱ�ȭ
                UpdateTile(x, y, null);
            }
        }

        // �� ���� �� ������ ����
        SaveTileData();
    }

    private void LoadExistingMap()
    {
        // ���� �ڽ� Tile ������Ʈ���� ������ �ε�
        foreach (Transform child in transform)
        {
            Tile tile = child.GetComponent<Tile>();
            if (tile != null)
            {
                Vector2Int gridPos = tile.GridPosition;
                if (IsValidGridPosition(gridPos.x, gridPos.y))
                {
                    tileDataArray[gridPos.x, gridPos.y] = tile.data;
                    tileObjects[gridPos] = child.gameObject;
                }
            }
        }
        SaveTileData(); // �ε� �� serializedTileData ������Ʈ
    }

    /// <summary>
    /// Ư�� ��ġ�� Ÿ�� ������ ������Ʈ. 
    /// null/empty��� ����, �ƴ϶�� ������ ������Ʈ
    /// </summary>
    public void UpdateTile(int x, int y, TileData newTileData)
    {
        if (!IsValidGridPosition(x, y)) return;

        if (newTileData == null || newTileData.terrain == TileData.TerrainType.Empty)
        {
            RemoveTile(x, y);
        }
        else
        {
            tileDataArray[x, y] = newTileData;
            CreateOrUpdateTileObject(x, y);
            if (newTileData.isStartPoint)
            {
                CreateSpawner(x, y);
            }
        }
    }

    /// <summary>
    /// �־��� ��ġ�� Ÿ�� ������Ʈ�� �����ϰų� ������Ʈ��.
    /// </summary>
    private void CreateOrUpdateTileObject(int x, int y)
    {
        Vector2Int gridPos = new Vector2Int(x, y);
        TileData tileData = tileDataArray[x, y];

        // �ش� ��ġ�� Ÿ���� �ִٸ�
        if (tileObjects.TryGetValue(gridPos, out GameObject existingTileObj))
        {
            UpdateExistingTile(existingTileObj, tileData, gridPos);
        }
        // ���ٸ�
        else
        {
            CreateNewTile(x, y, tileData);
        }
    }

    private void UpdateExistingTile(GameObject tileObj, TileData tileData, Vector2Int gridPos)
    {
        Tile tileComponent = tileObj.GetComponent<Tile>();
        if (tileComponent != null)
        {
            tileComponent.SetTileData(tileData, gridPos);
        }
    }

    private void CreateNewTile(int x, int y, TileData tileData)
    {
        if (tilePrefab == null) return;

        Vector2Int gridPos = new Vector2Int(x, y);
        Vector3 worldPos = GridToWorldPosition(gridPos);
        GameObject tileObj = Instantiate(tilePrefab, transform);

        tileObj.name = GenerateTileName(x, y, tileData);
        tileObj.transform.localPosition = worldPos;
        tileObj.layer = LayerMask.NameToLayer("Tile");

        Tile tileComponent = tileObj.GetComponent<Tile>();
        if (tileComponent != null)
        {
            tileComponent.SetTileData(tileData, gridPos);
        }
        else
        {
            Debug.LogError($"({x}, {y}) �׸��� �����ǿ� Ÿ�� ������Ʈ�� ����");
        }
        tileObjects[gridPos] = tileObj; 
    }

    /// </summary>
    /// Start Ÿ�Ͽ��� EnemySpawner �ڽ� ������Ʈ�� ��ġ�Ѵ�.
    /// SetTile�� �� �� �۵��ϹǷ�, ���� �ҷ����ų� �� �� ������ �۵���ų �ʿ� ����.
    /// </summary>
    private void CreateSpawner(int x, int y)
    {
        if (enemySpawnerPrefab == null)
        {
            Debug.LogError("Enemy spawner prefab is not set");
            return;
        }

        Vector2Int gridPos = new Vector2Int(x, y);
        if (tileObjects.TryGetValue(gridPos, out GameObject tileObj))
        {
            GameObject spawnerObj = Instantiate(enemySpawnerPrefab, tileObj.transform);
            spawnerObj.transform.localPosition = new Vector3(0, 0.5f, 0);
            spawnerObj.name = "EnemySpawner";
        }
        else
        {
            Debug.LogError($"Tile not found at position ({x}, {y}) for spawner creation");
        }
    }

    private string GenerateTileName(int x, int y, TileData data)
    {
        string baseName = $"Tile_{x}_{y}";
        if (data != null)
        {
            if (data.isStartPoint) return $"{baseName}_start";
            if (data.isEndPoint) return $"{baseName}_end";
        }
        return baseName;
    }

    public void RemoveTile(int x, int y)
    {
        Debug.Log($"RemoveTile : ���� ��ǥ {x}, {y}");
        if (!IsValidGridPosition(x, y)) return;

        Vector2Int gridPos = new Vector2Int(x, y);
        if (tileObjects.TryGetValue(gridPos, out GameObject tileObj))
        {
            Debug.Log($"RemoveTile : {gridPos} ���� ������ : �߰��� ������Ʈ : {tileObj.name}");
            DestroyImmediate(tileObj);
            tileObjects.Remove(gridPos);
        }
        tileDataArray[x, y] = null;
    }

    public TileData GetTileData(int x, int y)
    {
        if (IsValidGridPosition(x, y))
        {
            return tileDataArray[x, y];
        }
        return null;
    }

    /// <summary>
    /// �ش� "�׸���" ��ǥ�� ���� �ִ� Ÿ���� ��ȯ��. ���� ��ǥ�� �ƴ�!!
    /// </summary>
    public Tile GetTile(int x, int y)
    {
        if (!IsValidGridPosition(x, y)) return null;
        Vector2Int gridPos = new Vector2Int(x, y);
        return tileObjects.TryGetValue(gridPos, out GameObject tileObj) ? tileObj.GetComponent<Tile>() : null;
    }

    public bool IsValidGridPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public IEnumerable<Tile> GetAllTiles()
    {
        foreach (var tileObj in tileObjects.Values)
        {
            if (tileObj != null)
            {
                Tile tile = tileObj.GetComponent<Tile>();
                if (tile != null) yield return tile;
            }
        }
    }

    public Vector3 FindEndPoint()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tileDataArray[x, y]?.isEndPoint == true)
                {
                    return GridToWorldPosition(new Vector2Int(x, y));
                }
            }
        }
        return Vector3.zero;
    }


    /// <summary>
    /// ���� y ��ǥ�� 0���� ����. 
    /// 0.5�� �����ϰ� �ʹٸ� Vector3.Up * 0.5f�� �������.
    /// </summary>
    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x, 0, height - 1 - gridPos.y);
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.RoundToInt(worldPos.x), height - 1 - Mathf.RoundToInt(worldPos.z));
    }

    public void SetEnemySpawnerPrefab(GameObject prefab)
    {
        enemySpawnerPrefab = prefab;
    }

    // 2���� Ÿ�� �迭�� 1�������� ��ȯ�ؼ� ����
    public void SaveTileData()
    {
        serializedTileData = new TileData[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                serializedTileData[y * width + x] = tileDataArray[x, y];
            }
        }
    }

    // ����� �� ��� Ÿ�� ����
    public string GetTileDataDebugString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Map Tile Data:");
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                TileData tileData = tileDataArray[x, y];
                string tileInfo = tileData != null
                    ? $"{tileData.TileName} ({tileData.terrain})"
                    : "Empty";
                sb.AppendLine($"  Position ({x}, {y}): {tileInfo}");
            }
        }
        return sb.ToString();
    }

    public void RemoveAllTiles()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                RemoveTile(x, y);
            }
        }
    }
}