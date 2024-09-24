using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 1. 길찾기 알고리즘
/// 2. 바리케이드들의 정보 저장
/// </summary>
public class PathfindingManager : MonoBehaviour
{
    private List<Barricade> barricades = new List<Barricade>();
    public IReadOnlyList<Barricade> Barricades => barricades.AsReadOnly();
    public bool IsBarricadeDeployed => barricades.Count > 0;


    private static PathfindingManager instance; // 필드
    public static PathfindingManager Instance => instance;

    private Map currentMap;



    private void Awake()
    {
        if (instance == null) { instance = this; }
        else Destroy(gameObject);

        currentMap = FindObjectOfType<Map>();
        if (currentMap == null)
        {
            Debug.LogError("Map not found in the scene!");
        }
    }

    /// <summary>
    /// WorldPosition의 형태로 경로 반환
    /// </summary>
    public List<Vector3> FindPath(Vector3 startPos, Vector3 endPos)
    {
        Vector2Int startGrid = MapManager.Instance.ConvertToGridPosition(startPos);
        Vector2Int endGrid = MapManager.Instance.ConvertToGridPosition(endPos);

        // 경로들은 gridPosition으로 관리, 실제 이동은 Enemy에서 WorldPosition을 계산해서 그 위치로 이동한다
        List<Vector2Int> path = CalculatePath(startGrid, endGrid);

        if (path != null)
        {
            return ConvertToWorldPositions(path);
        }

        return null;
    }

    /// <summary>
    /// 노드의 형태로 경로 반환. 노드의 위치 정보는 gridPos임에 유의!
    /// </summary>
    public List<PathNode> FindPathAsNodes(Vector3 startPos, Vector3 endPos)
    {
        List<Vector3> worldPath = FindPath(startPos, endPos);
        if (worldPath == null) return null;

        List<PathNode> pathNodes = new List<PathNode>();
        foreach (Vector3 worldPos in worldPath)
        {
            Vector2Int gridPos = MapManager.Instance.ConvertToGridPosition(worldPos);
            Tile tile = currentMap.GetTile(gridPos.x, gridPos.y);

            PathNode node = new PathNode
            {
                tileName = tile.name,
                gridPosition = gridPos,
                waitTime = 0f
            };
            pathNodes.Add(node);
        }
        return pathNodes;
    }

    /// <summary>
    /// A* 알고리즘을 구현.
    /// 가장 낮은 FCost를 가진 타일들을 선택해서 탐색을 진행한다.
    /// 이웃 타일들을 평가하고, 더 나은 경로를 찾으면 업데이트한다. 목적지에 도달하면 RetracePath 메서드를 호출해 경로를 생성한다.
    /// 경로를 찾지 못하면 null을 반환한다.
    /// </summary>
    private List<Vector2Int> CalculatePath(Vector2Int start, Vector2Int end)
    {
        Tile startTile = currentMap.GetTile(start.x, start.y);
        Tile endTile = currentMap.GetTile(end.x, end.y);

        if (startTile == null || endTile == null)
        {
            Debug.LogWarning("Invalid Start or End Tile");
            return null;
        }

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
                return RetracePath(startTile, endTile);
            }

            foreach (Tile neighbor in GetNeighbors(currentTile))
            {
                if (neighbor.IsWalkable == false || closedSet.Contains(neighbor)) continue;

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
        return null;
    }

    private List<Vector2Int> RetracePath(Tile startTile, Tile endTile)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Tile currentTile = endTile;

        while (currentTile != null && currentTile != startTile)
        {
            path.Add(currentTile.GridPosition);
            currentTile = currentTile.Parent;
        }

        path.Add(startTile.GridPosition);
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

                if (currentMap.IsValidGridPosition(checkX, checkY))
                {
                    Tile neighbor = currentMap.GetTile(checkX, checkY);
                    if (neighbor != null && neighbor.data.isWalkable)
                    {
                        // 대각선 이동
                        if (x != 0 && y != 0)
                        {
                            // 대각선 이동 시, 양쪽 타일 모두 걸을 수 있어야 함
                            Tile SideA = currentMap.GetTile(tile.GridPosition.x + x, tile.GridPosition.y);
                            Tile SideB = currentMap.GetTile(tile.GridPosition.x, tile.GridPosition.y + y);

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

    /// <summary>
    /// 그리드 포지션인 경로 리스트를 월드 포지션으로 바꿈
    /// </summary>
    private List<Vector3> ConvertToWorldPositions(List<Vector2Int> gridPath)
    {
        return gridPath.Select(gridPos => MapManager.Instance.ConvertToWorldPosition(gridPos)).ToList();
    }

    public void AddBarricade(Barricade barricade)
    {
        if (!barricades.Contains(barricade))
        {
            barricades.Add(barricade);
        }
    }

    public void RemoveBarricade(Barricade barricade)
    {
        barricades.Remove(barricade);
    }

    private int GetPathLength(Vector3 start, Vector3 end)
    {
        List<PathNode> path = FindPathAsNodes(start, end);
        return path?.Count ?? int.MaxValue; // 경로가 있으면 경로 길이, null이면 int 최댓값
    }

    public Barricade GetNearestBarricade(Vector3 position)
    {
        return Barricades
                .OrderBy(b => GetPathLength(position, b.transform.position))
                .FirstOrDefault();
    }
}
