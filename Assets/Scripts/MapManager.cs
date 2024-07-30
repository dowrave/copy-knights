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
    private Map currentMap;
    //private Tile[,] tiles;
    [SerializeField] private List<PathData> availablePaths;

    //private int mapWidth, mapHeight;

    // currentMap 자체는 StageManager에서 관리, MapManager에서는 이를 받는 구조를 취한다.
    public void InitializeMap(Map map)
    {
        currentMap = map;

        if (currentMap != null)
        {
            currentMap.Initialize(currentMap.Width, currentMap.Height, true);
            InitializePaths(); 
        }
        else
        {
            Debug.LogError("Current Map is Not Assigned in MapManager");
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

    public Vector3 GetTilePosition(int x, int y)
    {
        return currentMap.GridToWorldPosition(new Vector2Int(x, y)) + Vector3.up * 0.5f;
    }

    public Tile GetTile(int x, int y)
    {
        return currentMap.GetTile(x, y);
    }

    public IEnumerable<Tile> GetAllTiles()
    {
        return currentMap.GetAllTiles();
    }

    public Vector3 GetEndPoint()
    {
        return currentMap.FindEndPoint();
    }

}