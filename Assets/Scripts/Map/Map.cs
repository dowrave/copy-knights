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
    private Dictionary<Vector2Int, GameObject> tileObjects = new Dictionary<Vector2Int, GameObject>(); // 좌표에 Tile 오브젝트 할당.
    private GameObject enemySpawnerPrefab = default!;

    // Map 프리팹에 저장하기 위해 이렇게 설정함
    [SerializeField] private List<EnemySpawner> enemySpawners = new List<EnemySpawner>();
    public IReadOnlyList<EnemySpawner> EnemySpawners => enemySpawners;


    // 스크립트 활성화마다 초기화를 확인한다
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

        // 불러오는 상황, enemySpawners에 정보가 없는 상황일 때
        GetEnemySpawners();
    }

    private void InitializeCommon(int width, int height)
    {
        this.width = width;
        this.height = height;
        tileDataArray = new TileData[width, height];
        tileObjects = new Dictionary<Vector2Int, GameObject>();
    }

    // 맵의 EnemySpawners들을 긁어옴. 
    private void GetEnemySpawners()
    {
        // enemySpawners가 빈 리스트일 때만 긁어온다
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
                // 기본 타일 데이터로 초기화
                UpdateTile(x, y, null);
            }
        }

        // 맵 생성 후 데이터 저장
        //SaveTileData();
    }

    private void LoadExistingMap()
    {
        // 기존 자식 Tile 오브젝트에서 데이터 로드
        foreach (Transform child in transform)
        {
            Tile tile = child.GetComponent<Tile>()!;
            Vector2Int gridPos = tile.GridPosition;
            if (IsValidGridPosition(gridPos.x, gridPos.y))
            {
                if (tile.data != null)
                {
                    // Null 허용 어서션(!)을 사용하여 경고 해결
                    tileDataArray![gridPos.x, gridPos.y] = tile.data;
                    tileObjects[gridPos] = child.gameObject;
                }
            }
        }

    }

    // 특정 위치의 타일 데이터 업데이트
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


    // 주어진 위치에 타일 오브젝트를 생성하거나 업데이트함.
    private void CreateOrUpdateTileObject(int x, int y)
    {
        Vector2Int gridPos = new Vector2Int(x, y);
        TileData? tileData = tileDataArray![x, y];

        if (tileData == null) return;

        // 해당 위치에 타일이 있다면
        if (tileObjects.TryGetValue(gridPos, out GameObject existingTileObj))
        {
            UpdateExistingTile(existingTileObj, tileData, gridPos);
        }
        // 없다면
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
            Debug.LogError($"({x}, {y}) 그리드 포지션에 타일 컴포넌트가 없음");
        }
        tileObjects[gridPos] = tileObj; 
    }

    // 시작 타일에 스포너 배치. SetTile에서 동작함.
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
            // 이 내부에 EnemySpawner라는 컴포넌트가 들어가 있음
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
            // 제거될 오브젝트의 자식에 EnemySpawner가 있다면 리스트에서 먼저 제거
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

        // 빈 타일 데이터를 입력: TerrainType.Empty와 "빈 타일" 이름 설정.
        tileDataArray![x, y] = new TileData() { terrain = TileData.TerrainType.Empty, TileName = "빈 타일" };
    }

    public TileData? GetTileData(int x, int y)
    {
        if (IsValidGridPosition(x, y))
        {
            return tileDataArray![x, y];
        }

        return null;
    }

    // 해당 그리드 좌표에 대한 타일을 반환합니다
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
        // 월드 y 좌표는 0으로 설정. 
        // 0.5로 설정하고 싶다면 Vector3.Up * 0.5f을 사용하자.
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

    // 디버그 용 모든 타일 보기
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