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
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        StageManager.Instance.OnMapLoaded += OnMapLoaded;
    }

    private void OnMapLoaded(Map map)
    {
        currentMap = map;
    }


    // WorldPosition의 형태로 경로 반환
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

    // 노드의 형태로 경로 반환. 노드의 위치 정보는 gridPos임에 유의!
    public List<PathNode> FindPathAsNodes(Vector3 startPos, Vector3 endPos)
    {
        List<Vector3> worldPath = FindPath(startPos, endPos);
        if (worldPath == null)
        {
            return null;
        }

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


    // A* 알고리즘을 구현.
    // 가장 낮은 FCost를 가진 타일들을 선택해서 탐색을 진행한다.
    // 이웃 타일들을 평가하고, 더 나은 경로를 찾으면 업데이트한다. 목적지에 도달하면 RetracePath 메서드를 호출해 경로를 생성한다.
    // 경로를 찾지 못하면 null을 반환한다.
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
                if (closedSet.Contains(neighbor)) continue; // 이미 포함된 타일
                if (neighbor.HasBarricade() && neighbor != endTile) continue; // 마지막 타일이 아니면서 바리케이드가 있는 타일

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

                    // 걸을 수 있는 타일 조건 (바리케이드와 관련된 조건은 밖에서 체크)
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

    /// <summary>
    /// 모든 바리케이드를 검색, 가장 가까운 바리케이드를 반환함
    /// </summary>
    public Barricade GetNearestBarricade(Vector3 position)
    {
        var barricadesWithDistances = barricades
            .Select(b => new
            {
                Barricade = b,
                Distance = GetPathLength(position, b.transform.position),
                BarricadePosition = b.transform.position
            })
            .ToList(); // 즉시 평가 때문에

        // 가장 가까운 바리케이드 반환
        return barricadesWithDistances
            .OrderBy(item => item.Distance)
            .Select(item => item.Barricade)
            .FirstOrDefault();
    }

    /// <summary>
    /// 경로의 노드 사이가 막혔는지를 검사
    /// </summary>
    public bool IsPathSegmentValid(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        RaycastHit hit;

        if (Physics.Raycast(start, direction.normalized, out hit, distance, LayerMask.GetMask("Deployable")))
        {
            // 레이캐스트 위치의 타일 확인
            Vector2Int tilePos = MapManager.Instance.ConvertToGridPosition(hit.point);
            Tile tile = MapManager.Instance.GetTile(tilePos.x, tilePos.y);

            if (tile != null && tile.IsWalkable == false)
            {
                return false;
            }
        }

        // 타일 기반 검사 추가
        List<Vector2Int> tilesOnPath = GetTilesOnPath(start, end);
        foreach (Vector2Int tilePos in tilesOnPath)
        {
            Tile tile = MapManager.Instance.GetTile(tilePos.x, tilePos.y);

            if (MapManager.Instance.GetTile(tilePos.x, tilePos.y).IsWalkable == false)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Bresenham's Line Algorithm을 이용, 경로 상의 모든 타일 좌표를 반환한다.
    /// </summary>
    public List<Vector2Int> GetTilesOnPath(Vector3 start, Vector3 end)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        // 3D => 2D 그리드 좌표 변환
        Vector2Int startTile = MapManager.Instance.ConvertToGridPosition(start);
        Vector2Int endTile = MapManager.Instance.ConvertToGridPosition(end);

        int x0 = startTile.x;
        int y0 = startTile.y;
        int x1 = endTile.x;
        int y1 = endTile.y;

        // 방향으로의 거리
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);

        // 이동 방향
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;

        // 오차 누적 변수
        int err = dx - dy;

        // 시작점 ~ 끝점까지 이동 
        while (true)
        {

            // 각 단계에서 현위치를 tiles 리스트에 추가
            tiles.Add(new Vector2Int(x0, y0));

            // 현재 위치 = 끝점이면 종료
            if (x0 == x1 && y0 == y1) break;

            // 오차를 사용해 방향 결정
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return tiles;
    }
}
