using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AI;

public class Map : MonoBehaviour
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    public int Width => width;
    public int Height => height;

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
                SetTile(x, y, null);
                //UpdateTileVisual(x, y);
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
    /// Ÿ�� ������ ����, �ʿ��� ��� Ÿ���� �����ϰų� ������
    /// </summary>
    public void SetTile(int x, int y, TileData newTileData)
    {
        if (!IsValidGridPosition(x, y)) return;

        if (newTileData == null || newTileData.terrain == TileData.TerrainType.Empty)
        {
            RemoveTile(x, y);
        }
        else
        {
            tileDataArray[x, y] = newTileData;
            UpdateTileVisual(x, y);
            if (newTileData.isStartPoint)
            {
                CreateSpawner(x, y);
            }
        }
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

    /// <summary>
    /// ���� �� Ÿ�Ͽ� ���� �ð��� ǥ���� ������Ʈ�ϰų� �����Ѵ�.
    /// 1. ����� ������ - ���� Ÿ�� ������Ʈ ����ȭ
    /// 2. ������ Ÿ�� ����, �� �̻� �ʿ���� Ÿ�� ����
    /// 3. �� Ÿ���� �ð��� ǥ���� ����� �����Ϳ� ��ġ�ϵ��� ����
    /// </summary>
    private void UpdateTileVisual(int x, int y)
    {
        Vector2Int gridPos = new Vector2Int(x, y);
        TileData tileData = tileDataArray[x, y];

        if (tileObjects.TryGetValue(gridPos, out GameObject existingTileObj))
        {
            if (tileData == null || tileData.terrain == TileData.TerrainType.Empty)
            {
                RemoveTile(x, y);
            }
            else
            {
                Tile tileComponent = existingTileObj.GetComponent<Tile>();
                tileComponent.SetTileData(tileData, gridPos);
            }
        }
        else if (tileData != null && tileData.terrain != TileData.TerrainType.Empty)
        {
            CreateTile(x, y, tileData);
        }
    }

    /// <summary>
    /// ���� Ÿ�� ������Ʈ�� �����ϰ� �����ϴ� ������ �����Ѵ�.
    /// </summary>
    private void CreateTile(int x, int y, TileData data)
    {
        if (tilePrefab == null) return;

        Vector2Int gridPos = new Vector2Int(x, y);
        Vector3 worldPos = GridToWorldPosition(gridPos);
        GameObject tileObj = Instantiate(tilePrefab, transform);
        tileObj.name = GenerateTileName(x, y, data);
        tileObj.transform.localPosition = worldPos;

        Tile tileComponent = tileObj.GetComponent<Tile>();
        if (tileComponent != null)
        {
            tileComponent.SetTileData(data, gridPos);
        }
        else
        {
            Debug.LogError($"Tile component not found on prefab for position ({x}, {y})");
        }

        tileObjects[gridPos] = tileObj;
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
                    return GridToWorldPosition(new Vector2Int(x, y)) + Vector3.up * 0.5f;
                }
            }
        }
        return Vector3.zero;
    }
    //1
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