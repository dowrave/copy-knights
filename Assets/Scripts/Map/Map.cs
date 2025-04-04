using UnityEngine;
using System.Collections.Generic;
using System.Text;
using UnityEditor;


public class Map : MonoBehaviour
{

    [Header("Map Identity")]
    [SerializeField] private string mapId = string.Empty;
    public string Mapid => mapId;

    [SerializeField] private int width;
    [SerializeField] private int height;
    public int Width => width;
    public int Height => height;

    [SerializeField] private Vector3 cameraPosition;
    private Vector3 cameraRotation = new Vector3(70, 0, 0);
    public Vector3 CameraPosition => cameraPosition;
    public Vector3 CameraRotation => cameraRotation;

    private TileData?[,]? tileDataArray; 
    private Dictionary<Vector2Int, GameObject> tileObjects = new Dictionary<Vector2Int, GameObject>(); // ��ǥ�� Tile ������Ʈ �Ҵ�.
    private GameObject enemySpawnerPrefab = default!;

    // Map �����տ� �����ϱ� ���� �̷��� ������
    [SerializeField] private List<EnemySpawner> enemySpawners = new List<EnemySpawner>();
    public IReadOnlyList<EnemySpawner> EnemySpawners => enemySpawners;


    // ��ũ��Ʈ Ȱ��ȭ���� �ʱ�ȭ�� Ȯ���Ѵ�
    private void OnValidate()
    {
        InitializeOnEditor(width, height, true);
    }

    public void InitializeOnStage(int width, int height)
    {
        InitializeCommon(width, height);
        LoadExistingMap();
        GetEnemySpawners();
    }

    public void InitializeOnEditor(int width, int height, bool load = false)
    {
        InitializeCommon(width, height);

        if (load)
        {
            LoadExistingMap();
        }
        else
        {
            CreateNewMap();
        }

        if (enemySpawnerPrefab == null)
        {
            enemySpawnerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemySpawner.prefab");
        }

        // �ҷ����� ��Ȳ, enemySpawners�� ������ ���� ��Ȳ�� ��
        GetEnemySpawners();
    }

    private void InitializeCommon(int width, int height)
    {
        this.width = width;
        this.height = height;
        tileDataArray = new TileData[width, height];
        tileObjects = new Dictionary<Vector2Int, GameObject>();
    }

    // ���� EnemySpawners���� �ܾ��. 
    private void GetEnemySpawners()
    {
        // enemySpawners�� �� ����Ʈ�� ���� �ܾ�´�
        if (enemySpawners.Count == 0)
        {
            EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
            foreach (EnemySpawner spawner in spawners)
            {
                enemySpawners.Add(spawner);
            }
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
        //SaveTileData();
    }

    private void LoadExistingMap()
    {
        // ���� �ڽ� Tile ������Ʈ���� ������ �ε�
        foreach (Transform child in transform)
        {
            Tile tile = child.GetComponent<Tile>()!;
            Vector2Int gridPos = tile.GridPosition;
            if (IsValidGridPosition(gridPos.x, gridPos.y))
            {
                if (tile.data != null)
                {
                    // Null ��� ���(!)�� ����Ͽ� ��� �ذ�
                    tileDataArray![gridPos.x, gridPos.y] = tile.data;
                    tileObjects[gridPos] = child.gameObject;
                }
            }
        }

    }

    // Ư�� ��ġ�� Ÿ�� ������ ������Ʈ
    public void UpdateTile(int x, int y, TileData? newTileData)
    {
        if (!IsValidGridPosition(x, y)) return;

        if (newTileData == null || newTileData.terrain == TileData.TerrainType.Empty)
        {
            RemoveTile(x, y);
        }
        else
        {
            tileDataArray![x, y] = newTileData;
            CreateOrUpdateTileObject(x, y);
            if (newTileData.isStartPoint)
            {
                CreateSpawner(x, y);
            }
        }
    }


    // �־��� ��ġ�� Ÿ�� ������Ʈ�� �����ϰų� ������Ʈ��.
    private void CreateOrUpdateTileObject(int x, int y)
    {
        Vector2Int gridPos = new Vector2Int(x, y);
        TileData? tileData = tileDataArray![x, y];

        if (tileData == null) return;

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
        Vector2Int gridPos = new Vector2Int(x, y);
        Vector3 worldPos = GridToWorldPosition(gridPos);

        GameObject tilePrefab = tileData.tilePrefab;
        if (tilePrefab == null) return;
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

    // ���� Ÿ�Ͽ� ������ ��ġ. SetTile���� ������.
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
            // �� ���ο� EnemySpawner��� ������Ʈ�� �� ����
            GameObject spawnerObj = Instantiate(enemySpawnerPrefab, tileObj.transform);
            spawnerObj.transform.localPosition = new Vector3(0, 0.5f, 0);
            spawnerObj.name = $"EnemySpawner({x}-{y})";

            EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();
            if (spawner != null)
            {
                enemySpawners.Add(spawner);
            }
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
        if (!IsValidGridPosition(x, y)) return;

        Vector2Int gridPos = new Vector2Int(x, y);
        if (tileObjects.TryGetValue(gridPos, out GameObject tileObj))
        {
            // ���ŵ� ������Ʈ�� �ڽĿ� EnemySpawner�� �ִٸ� ����Ʈ���� ���� ����
            EnemySpawner spawner = tileObj.GetComponentInChildren<EnemySpawner>();
            if (spawner != null)
            {
                if (enemySpawners != null && enemySpawners.Contains(spawner))
                {
                    enemySpawners.Remove(spawner);
                }
            }

            DestroyImmediate(tileObj);
            tileObjects.Remove(gridPos);
        }

        // �� Ÿ�� �����͸� �Է�: TerrainType.Empty�� "�� Ÿ��" �̸� ����.
        tileDataArray![x, y] = new TileData() { terrain = TileData.TerrainType.Empty, TileName = "�� Ÿ��" };
    }

    public TileData? GetTileData(int x, int y)
    {
        if (IsValidGridPosition(x, y))
        {
            return tileDataArray![x, y];
        }

        return null;
    }

    // �ش� �׸��� ��ǥ�� ���� Ÿ���� ��ȯ�մϴ�
    public Tile? GetTile(int gridX, int gridY)
    {
        if (!IsValidGridPosition(gridX, gridY)) return null;

        if (IsTileAt(gridX, gridY))
        {
            Vector2Int gridPos = new Vector2Int(gridX, gridY);
            GameObject tileObj = tileObjects[gridPos];
            return tileObj.GetComponent<Tile>();
        }
        return null;
    }

    public bool IsTileAt(int gridX, int gridY)
    {
        return tileObjects.ContainsKey(new Vector2Int(gridX, gridY));
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

    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        // ���� y ��ǥ�� 0���� ����. 
        // 0.5�� �����ϰ� �ʹٸ� Vector3.Up * 0.5f�� �������.
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

    // ����� �� ��� Ÿ�� ����
    public string GetTileDataDebugString()
    {
        if (tileDataArray != null) 
        { 
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Map Tile Data:");
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    TileData? tileData = tileDataArray![x, y];
                    string tileInfo = tileData != null
                        ? $"{tileData.TileName} ({tileData.terrain})"
                        : "Empty";
                    sb.AppendLine($"  Position ({x}, {y}): {tileInfo}");
                }
            }
            return sb.ToString();
        }
        return string.Empty;
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