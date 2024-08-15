using UnityEngine;
using System.Collections.Generic;

/*
 MapManager의 역할
1. 맵 데이터 로드 및 관리
2. 타일 정보 제공
3. 경로 찾기 기능 제공
4. 맵 관련 유틸리티 함수 제공(월드 <-> 그리드 좌표)
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

    // currentMap 자체는 StageManager에서 관리, MapManager에서는 이를 받는 구조를 취한다.
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
            Debug.LogError("카메라 매니저 인스턴스를 찾을 수 없었음");
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
    /// 해당 오브젝트의 월드 좌표를 인풋으로 받아 그리드 좌표를 반환함
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