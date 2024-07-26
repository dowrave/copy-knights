using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFindingManager : MonoBehaviour
{

    private static PathFindingManager instance; // 필드
    public static PathFindingManager Instance => instance;

    private MapManager mapManager;

    private void Awake()
    {
        if (instance == null) { instance = this; }
        else Destroy(gameObject);

        mapManager = FindObjectOfType<MapManager>();
        if (mapManager == null)
        {
            Debug.LogError("MapManager not found in the scene!");
        }
    }


    /// <summary>
    /// A* 알고리즘을 구현.
    /// 가장 낮은 FCost를 가진 타일들을 선택해서 탐색을 진행한다.
    /// 이웃 타일들을 평가하고, 더 나은 경로를 찾으면 업데이트한다. 목적지에 도달하면 RetracePath 메서드를 호출해 경로를 생성한다.
    /// 경로를 찾지 못하면 null을 반환한다.
    /// </summary>
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Debug.Log($"Attempting to find path from {startPos} to {targetPos}");
        Vector2Int start = new Vector2Int(Mathf.RoundToInt(startPos.x), Mathf.RoundToInt(startPos.z));
        Vector2Int end = new Vector2Int(Mathf.RoundToInt(targetPos.x), Mathf.RoundToInt(targetPos.z));

        Debug.Log($"PathFindingManager - FindPath start 값 : {start}");
        Debug.Log($"PathFindingManager - FindPath end 값 : {end}");

        Tile startTile = mapManager.GetTile(start.x, start.y);
        Tile endTile = mapManager.GetTile(end.x, end.y);

        if (startTile == null) { Debug.LogWarning("startTile이 null!"); };
        if (endTile == null) { Debug.LogWarning("endTile이 null!"); };

        Debug.Log($"Start tile: {startTile?.GridPosition}, End tile: {endTile?.GridPosition}");
        Debug.Log($"Start tile walkable: {startTile?.data.isWalkable}, End tile walkable: {endTile?.data.isWalkable}");

        List<Tile> openSet = new List<Tile>();
        HashSet<Tile> closedSet = new HashSet<Tile>();
        openSet.Add(startTile);

        while (openSet.Count > 0)
        {
            Tile currentTile = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentTile.FCost || (openSet[i].FCost == currentTile.FCost && openSet[i].HCost < currentTile.HCost))
                {
                    currentTile = openSet[i];
                }
            }

            openSet.Remove(currentTile);
            closedSet.Add(currentTile);

            if (currentTile == endTile)
            {
                return RetracePath(startTile, endTile); // 역순으로 경로를 다시 만듦
            }

            foreach (Tile neighbor in GetNeighbors(currentTile))
            {
                if (!neighbor.data.isWalkable || closedSet.Contains(neighbor)) continue;

                int newMovementCostToNeighbor = currentTile.GCost + GetDistance(currentTile, neighbor);
                if (newMovementCostToNeighbor < neighbor.GCost || !openSet.Contains(neighbor))
                {
                    neighbor.GCost = newMovementCostToNeighbor;
                    neighbor.HCost = GetDistance(neighbor, endTile);
                    neighbor.Parent = currentTile;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }
        Debug.LogWarning("No path found!");
        return null; // 경로가 없는 경우
    }

    /// <summary>
    /// 끝타일부터 시작 타일까지 Parent를 따라 역순으로 경로를 만듦
    /// </summary>
    private List<Vector3> RetracePath(Tile startTile, Tile endTile)
    {
        List<Vector3> path = new List<Vector3>();
        Tile currentTile = endTile;

        while (currentTile != null && currentTile != startTile)
        {
            path.Add(mapManager.GetTilePosition(currentTile.GridPosition.x, currentTile.GridPosition.y));
            currentTile = currentTile.Parent;
        }

        if (currentTile == null)
        {
            Debug.LogError("Path is incomplete!");
            return null;
        }

        path.Add(mapManager.GetTilePosition(startTile.GridPosition.x, startTile.GridPosition.y));
        path.Reverse();
        return path;
    }

    /// <summary>
    /// 주어진 타일의 이웃 타일 8칸 중, 갈 수 있는 경우에만 이웃으로 추가
    /// </summary>
    private List<Tile> GetNeighbors(Tile tile)
    {
        List<Tile> neighbors = new List<Tile>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int checkX = tile.GridPosition.x + x;
                int checkY = tile.GridPosition.y + y;

                Tile neighbor = mapManager.GetTile(checkX, checkY);

                if (neighbor != null && neighbor.data.isWalkable)
                {
                    // 대각선 이동
                    if (x != 0 && y != 0)
                    {
                        // 대각선 이동 시, 양쪽 타일 모두 걸을 수 있어야 함
                        Tile SideA = mapManager.GetTile(tile.GridPosition.x + x, tile.GridPosition.y);
                        Tile SideB = mapManager.GetTile(tile.GridPosition.x, tile.GridPosition.y + y);

                        if (SideA != null && SideB != null && SideA.data.isWalkable && SideB.data.isWalkable)
                        {
                            neighbors.Add(neighbor);
                        }

                    }
                    // 직선 이동
                    else
                    { 
                        neighbors.Add(neighbor);
                    }
                }
            }
        }
        return neighbors;
    }

    /// <summary>
    /// 거리를 재는 휴리스틱 함수
    /// </summary>
    private int GetDistance(Tile tileA, Tile tileB)
    {
        int dstX = Mathf.Abs(tileA.GridPosition.x - tileB.GridPosition.x);
        int dstY = Mathf.Abs(tileA.GridPosition.y - tileB.GridPosition.y);

        if (dstX > dstY)
            return 15 * dstY + 10 * (dstX - dstY);
        return 15 * dstX + 10 * (dstY - dstX);
    }
}
