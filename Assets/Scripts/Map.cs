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
    private Dictionary<Vector2Int, GameObject> tileObjects; // 좌표에 Tile 오브젝트 할당.



    // 스크립트 활성화마다 초기화를 확인한다
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
                // 기본 타일 데이터로 초기화
                SetTile(x, y, null);
                //UpdateTileVisual(x, y);
            }
        }

        // 맵 생성 후 데이터 저장
        SaveTileData();
    }

    private void LoadExistingMap()
    {
        // 기존 자식 Tile 오브젝트에서 데이터 로드
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
        SaveTileData(); // 로드 후 serializedTileData 업데이트
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


    /// </summary>
    /// Start 타일에는 EnemySpawner 자식 오브젝트를 배치한다.
    /// SetTile을 할 때 작동하므로, 맵을 불러오거나 할 때 별도로 작동시킬 필요 없다.
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
    /// 맵의 각 타일에 대한 시각적 표현을 업데이트하거나 생성한다.
    /// 1. 저장된 데이터 - 씬의 타일 오브젝트 동기화
    /// 2. 누락된 타일 생성, 더 이상 필요없는 타일 제거
    /// 3. 각 타일의 시각적 표현이 저장된 데이터와 일치하도록 보장
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
        Debug.Log($"RemoveTile : 들어온 좌표 {x}, {y}");
        if (!IsValidGridPosition(x, y)) return;

        Vector2Int gridPos = new Vector2Int(x, y);
        if (tileObjects.TryGetValue(gridPos, out GameObject tileObj))
        {
            Debug.Log($"RemoveTile : {gridPos} 값이 들어왔음 : 발견한 오브젝트 : {tileObj.name}");
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

    // 2차원 타일 배열을 1차원으로 변환해서 저장
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

    // 디버그 용 모든 타일 보기
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