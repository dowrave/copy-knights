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
    //[SerializeField] private GameObject mapPrefab;
    [SerializeField] private Map currentMap;
    private Tile[,] tiles;
    [SerializeField] private List<PathData> availablePaths;


    private int mapWidth, mapHeight;

    public void InitializeMap()
    {
        LoadMapFromScene();
        //LoadMapFromPrefabOrScene();
        InitializePaths();
    }


    private void Start()
    {
        LoadMapFromScene();
        //currentMap = FindObjectOfType<Map>(); // 하이어라키에 맵 오브젝트가 있는지 체크
        
        //if (currentMap == null) // 없다면 프리팹에서 맵 로드
        //{
        //    LoadMapFromPrefab();
        //}
        //else // 있다면 씬에서 맵 로드
        //{
        //    LoadMapFromScene();
        //}
        InitializePaths();
    }
    //private void LoadMapFromPrefabOrScene()
    //{
    //    currentMap = FindObjectOfType<Map>();

    //    if (currentMap == null)
    //    {
    //        LoadMapFromPrefab();
    //    }
    //    else
    //    {
    //        LoadMapFromScene();
    //    }
    //}

    // 씬에 맵을 같이 띄웠다면, 여기서 가져온다
    private void LoadMapFromScene()
    {
        currentMap = FindObjectOfType<Map>();

        if (currentMap != null)
        {
            mapWidth = currentMap.Width;
            mapHeight = currentMap.Height;

            SetupTilesArray();

            // 맵 초기화
            currentMap.Initialize(mapWidth, mapHeight, true, this);
            InitializeEnemySpawners();
        }

    }

    //private void LoadMapFromPrefab()
    //{
    //    if (mapPrefab != null)
    //    {
    //        GameObject mapInstance = Instantiate(mapPrefab, transform);
    //        currentMap = mapInstance.GetComponent<Map>();

    //        if (currentMap != null)
    //        {
    //            mapWidth = currentMap.Width;
    //            mapHeight = currentMap.Height;

    //            SetupTilesArray();

    //            // 맵 초기화
    //            currentMap.Initialize(mapWidth, mapHeight, true, this);

    //            InitializeEnemySpawners();
    //        }
    //    }
    //}

    private void SetupTilesArray()
    {
        tiles = new Tile[mapWidth, mapHeight];

        // Map의 GetAllTiles 메소드를 사용하여 모든 타일을 가져옵니다.
        foreach (Tile tile in currentMap.GetAllTiles())
        {
            Vector2Int gridPos = tile.GridPosition;
            if (IsValidGridPosition(gridPos))
            {
                tiles[gridPos.x, gridPos.y] = tile;
            }
            else
            {
                Debug.LogWarning($"타일 위치가 맵 범위를 벗어남: {gridPos}");
            }
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