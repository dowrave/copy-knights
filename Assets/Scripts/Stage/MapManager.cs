using UnityEngine;
using System.Collections.Generic;

/*
 MapManager�� ����
1. �� ������ �ε� �� ����
2. Ÿ�� ���� ����
3. ��� ã�� ��� ����
4. �� ���� ��ƿ��Ƽ �Լ� ����(���� <-> �׸��� ��ǥ)
 */
public class MapManager : MonoBehaviour
{ 
    public static MapManager Instance { get; private set; }
    private Map currentMap;
    [SerializeField] private List<PathData> availablePaths;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeMap();
    }

    //private int mapWidth, mapHeight;

    // currentMap ��ü�� StageManager���� ����, MapManager������ �̸� �޴� ������ ���Ѵ�.
    public void InitializeMap()
    {
        currentMap = FindObjectOfType<Map>();
        if (currentMap != null)
        {
            currentMap.Initialize(currentMap.Width, currentMap.Height, true);
            SpawnerManager.Instance.Initialize(currentMap);
            //CameraManager.Instance.AdjustCameraToMap(currentMap.Width, currentMap.Height);
            InitializeCameraManager();
            InitializePaths(); 
        }
        else
        {
            Debug.LogError("Current Map is Not Assigned in MapManager");
        }
    }

    private void InitializeCameraManager()
    {
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.SetupForMap(currentMap);
        }
        else
        {
            Debug.LogError("ī�޶� �Ŵ��� �ν��Ͻ��� ã�� �� ������");
        }
    }

    private void InitializePaths()
    {
        foreach (PathData path in availablePaths)
        {
            ValidatePath(path);
        }
    }

    private void ValidatePath(PathData path)
    {
        foreach (PathNode node in path.nodes)
        {
            Vector2Int gridPos = currentMap.WorldToGridPosition(node.position);
            if (!currentMap.IsValidGridPosition(gridPos.x, gridPos.y))
            {
                Debug.LogWarning($"Invalid path node position in path {path.name}: {node.position}");
            }
        }
    }

    public bool IsPositionWalkable(Vector3 worldPosition)
    {
        Vector2Int gridPos = currentMap.WorldToGridPosition(worldPosition);
        TileData tileData = currentMap.GetTileData(gridPos.x, gridPos.y);
        return tileData != null && tileData.isWalkable;
    }


    public Vector3 GetTilePosition(int gridX, int gridY)
    {
        return currentMap.GridToWorldPosition(new Vector2Int(gridX, gridY)) + Vector3.up * 0.5f;
    }


    public Tile GetTile(int worldX, int worldZ)
    {
        return currentMap.GetTile(worldX, worldZ);
    }

    public IEnumerable<Tile> GetAllTiles()
    {
        return currentMap.GetAllTiles();
    }

    /// <summary>
    /// �ش� ������Ʈ�� ���� ��ǥ�� ��ǲ���� �޾� �׸��� ��ǥ�� ��ȯ��
    /// </summary>
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        return currentMap.WorldToGridPosition(worldPosition);
    }


    public Vector3 GetEndPoint()
    {
        return currentMap.FindEndPoint();
    }

    public Map GetCurrentMap()
    {
        return currentMap;
    }

    public (Vector3 position, Vector3 rotation) GetCurrentMapCameraSettings()
    {
        if (currentMap != null)
        {
            return (currentMap.CameraPosition, currentMap.CameraRotation);
        }
        return (Vector3.zero, Vector3.zero);
    }
}