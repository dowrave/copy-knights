using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class Map : MonoBehaviour
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private TileData[,] tileDataArray;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject enemySpawnerPrefab;

    public int Width => width;
    public int Height => height;

    private Dictionary<Vector2Int, GameObject> tileObjects;

    public void Initialize(int width, int height, bool load = false)
    {
        this.width = width;
        this.height = height;
        tileDataArray = new TileData[width, height];
        tileObjects = new Dictionary<Vector2Int, GameObject>();

        if (tilePrefab == null)
        {
            tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tiles/DefaultTile.prefab");
            if (tilePrefab == null)
            {
                Debug.LogError("Default tile prefab not found at Assets/Prefabs/Tiles/DefaultTile.prefab");
            }
        }

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
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SetTile(x, y, null);
            }
        }
    }

    private void LoadExistingMap()
    {
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent<Tile>(out Tile tile))
            {
                Vector2Int gridPos = tile.GridPosition;
                if (IsValidGridPosition(gridPos.x, gridPos.y))
                {
                    tileDataArray[gridPos.x, gridPos.y] = tile.data;
                    tileObjects[gridPos] = child.gameObject;
                }
                else
                {
                    Debug.LogWarning($"Invalid tile position found: {gridPos}");
                }
            }
        }
    }

    /// <summary>
    /// 타일 데이터 설정, 필요한 경우 타일을 생성하거나 제거함
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
    /// 실제 타일 오브젝트를 생성하고 설정하는 역할을 수행한다.
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
        if (!IsValidGridPosition(x, y)) return;

        Vector2Int gridPos = new Vector2Int(x, y);
        if (tileObjects.TryGetValue(gridPos, out GameObject tileObj))
        {
            DestroyImmediate(tileObj);
            tileObjects.Remove(gridPos);
        }
        tileDataArray[x, y] = null;
    }

    public TileData GetTileData(int x, int y)
    {
        return IsValidGridPosition(x, y) ? tileDataArray[x, y] : null;
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
}