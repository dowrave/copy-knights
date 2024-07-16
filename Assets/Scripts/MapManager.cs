using UnityEngine;
using System.Collections.Generic;

/*
 MapManager�� ����
1. �� ������ �ε� �� ����
2. Ÿ�� ���� ������
3. ��� ã�� ��� ����
4. �� ���� ��ƿ��Ƽ �Լ� ����(���� <-> �׸��� ��ǥ)
 */
public class MapManager : MonoBehaviour
{
    [SerializeField] private GameObject mapPrefab;
    private Map currentMap;
    private Tile[,] tiles;
    [SerializeField] private List<PathData> availablePaths;


    private int mapWidth, mapHeight;

    public void InitializeMap()
    {
        LoadMapFromPrefabOrScene();
        InitializePaths();
    }


    private void Start()
    {
        currentMap = FindObjectOfType<Map>(); // ���̾��Ű�� �� ������Ʈ�� �ִ��� üũ

        if (currentMap == null) // ���ٸ� �����տ��� �� �ε�
        {
            LoadMapFromPrefab();
        }
        else // �ִٸ� ������ �� �ε�
        {
            LoadMapFromScene();
        }
        InitializePaths();
    }
    private void LoadMapFromPrefabOrScene()
    {
        currentMap = FindObjectOfType<Map>();
        Debug.Log($"�� �Ŵ��� currentMap : {currentMap}");

        if (currentMap == null)
        {
            LoadMapFromPrefab();
        }
        else
        {
            LoadMapFromScene();
        }
    }

    // ���� ���� ���� ����ٸ�, ���⼭ �����´�
    private void LoadMapFromScene()
    {
        Debug.Log("�� �Ŵ��� : ������ �� �ε�");
        // Map ������Ʈ���� Ÿ�� ���� �ε�
        Tile[] allTiles = currentMap.GetComponentsInChildren<Tile>();

        SetupMapDimensions(allTiles);
        SetupTilesArray(allTiles);

        // Map �ʱ�ȭ
        currentMap.Initialize(mapWidth, mapHeight, true, this);

        InitializeEnemySpawners();
    }

    private void LoadMapFromPrefab()
    {
        Debug.Log("�� �Ŵ��� : �����տ��� �� �ε�");

        if (mapPrefab != null)
        {
            GameObject mapInstance = Instantiate(mapPrefab, transform);
            currentMap = mapInstance.GetComponent<Map>();
            if (currentMap != null)
            {
                Tile[] allTiles = mapInstance.GetComponentsInChildren<Tile>();

                SetupMapDimensions(allTiles);
                SetupTilesArray(allTiles);

                // �� �ʱ�ȭ
                currentMap.Initialize(mapWidth, mapHeight, true, this);

                InitializeEnemySpawners();
            }
        }
    }

    private void SetupMapDimensions(Tile[] allTiles)
    {
        int maxX = 0, maxY = 0;
        foreach (Tile tile in allTiles)
        {
            Vector2Int gridPos = tile.GridPosition;
            maxX = Mathf.Max(maxX, gridPos.x);
            maxY = Mathf.Max(maxY, gridPos.y);

            mapWidth = maxX + 1;
            mapHeight = maxY + 1;
        }
    }

    private void SetupTilesArray(Tile[] allTiles)
    {
        tiles = new Tile[mapWidth, mapHeight];
        foreach (Tile tile in allTiles)
        {
            Vector2Int gridPos = tile.GridPosition;
            tiles[gridPos.x, gridPos.y] = tile;
        }
    }

    private void InitializePaths()
    {
        foreach (PathData path in availablePaths)
        {
            ValidatePath(path);
        }
    }
    
    private void InitializeEnemySpawners()
    {
        EnemySpawner[] spawners = currentMap.GetComponentsInChildren<EnemySpawner>();
        foreach (var spawner in spawners)
        {
            spawner.Initialize(this);
        }
    }

    private void ValidatePath(PathData path)
    {
        foreach (PathNode node in path.nodes)
        {
            Vector2Int gridPos = WorldToGridPosition(node.position);
            if (!IsValidGridPosition(gridPos))
            {
                Debug.LogWarning($"Invalid path node position in path {path.name}: {node.position}");
            }
        }
    }

    private Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        return new Vector2Int(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z));
    }

    private bool IsValidGridPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < mapWidth && gridPos.y >= 0 && gridPos.y < mapHeight;
    }

    public bool IsPositionWalkable(Vector3 worldPosition)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPosition);
        return IsValidGridPosition(gridPos) && tiles[gridPos.x, gridPos.y].data.isWalkable;
    }

    public Vector3 GetTilePosition(int x, int y)
    {
        if (IsValidGridPosition(new Vector2Int(x, y)) && tiles[x, y] != null)
        {
            return tiles[x, y].transform.position + Vector3.up * 0.5f;
        }
        return Vector3.zero;
    }

    public Tile GetTile(int x, int y)
    {
        if (IsValidGridPosition(new Vector2Int(x, y)))
        {
            return tiles[x, y];
        }
        return null;
    }

    public IEnumerable<Tile> GetAllTiles()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (tiles[x, y] != null)
                {
                    yield return tiles[x, y];
                }
            }
        }
    }

    public Vector3 GetEndPoint()
    {
        return currentMap.FindEndPoint();
    }

    
}