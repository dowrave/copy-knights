using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MapEditorWindow : EditorWindow
{
    public int mapWidth = 5;
    public int mapHeight = 5;
    private Map currentMap;
    private TileData selectedTileData;
    private Vector2 scrollPosition;
    private List<TileData> availableTileData = new List<TileData>();

    private GameObject loadedMapPrefab;

    private const string MAP_OBJECT_NAME = "EditorMap"; 
    private const string MAP_PREFAB_PATH = "Assets/Prefabs/Map";

    [MenuItem("Window/Map Editor")]
    public static void ShowWindow()
    {
        GetWindow<MapEditorWindow>("Map Editor");
    }

    private void OnEnable()
    {
        LoadAvailableTileData();
        //InitializeMap();

    }

    //private void OnDisable()
    //{
    //    if (currentMap != null)
    //    {
    //        DestroyImmediate(currentMap.gameObject);
    //    }
    //}

    // 사전에 ScriptableObject로 생성한 tileData들을 불러오는 로직
    private void LoadAvailableTileData()
    {
        availableTileData.Clear();
        string[] guids = AssetDatabase.FindAssets("t:TileData", new[] { "Assets/ScriptableObjects/Tiledata" });
        foreach (string guid in guids)
        {

            string path = AssetDatabase.GUIDToAssetPath(guid);
            TileData tileData = AssetDatabase.LoadAssetAtPath<TileData>(path);
            if (tileData != null)
            {
                availableTileData.Add(tileData);
            }
        }
        if (availableTileData.Count > 0 )
        {
            selectedTileData = availableTileData[0];
        }
    }


    private void InitializeNewMap()
    {
        // 기존 맵 제거 
        RemoveExistingMaps();

        // 새 맵 게임 오브젝트 생성
        GameObject mapObject = new GameObject(MAP_OBJECT_NAME);
        currentMap = mapObject.AddComponent<Map>();

        // 새 맵 초기화(load = false)
        currentMap.Initialize(mapWidth, mapHeight, false);

        //for (int x = 0; x < mapWidth; x++)
        //{
        //    for (int y = 0; y < mapHeight; y++)
        //    {
        //        TileData emptyTile = availableTileData.Find(t => t.terrain == TileData.TerrainType.Empty);
        //        currentMap.SetTile(x, y, emptyTile);
        //    }
        //}

        // 에디터 UI 갱신
        Repaint();
        SceneView.RepaintAll();
    }

    private void OnGUI()
    {
        
        if (currentMap == null)
        { 
            GUILayout.Label("맵이 없습니다. 새로운 맵을 생성하거나 로드하세요.", EditorStyles.boldLabel);
            if (GUILayout.Button("Create New Map"))
            {
                InitializeNewMap();
            }
            if (GUILayout.Button("Load Map"))
            {
                LoadMap();
            }
            return ;
        }

        GUILayout.Label("Map Editor", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        int newWidth = EditorGUILayout.IntField("Width", mapWidth);
        int newHeight = EditorGUILayout.IntField("Height", mapHeight);
        EditorGUILayout.EndHorizontal();

        // 맵 크기 변경 시 실제 맵에도 변경이 발생
        if (newWidth != mapWidth || newHeight != mapHeight)
        {
            ResizeMap(newWidth, newHeight);
        }

        selectedTileData = (TileData)EditorGUILayout.ObjectField("Selected Tile", selectedTileData, typeof(TileData), false);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // 맵 그리드 그리기
        DrawMapGrid();

        if (GUILayout.Button("Reset Map"))
        {
            InitializeNewMap();
        }

        if (GUILayout.Button("Load Map"))
        {
            LoadMap();
        }

        if (GUILayout.Button("Save Map")) 
        {
            SaveMap();
        }
    }

    private void DrawMapGrid()
    {
        for (int y = 0; y < mapHeight; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < mapWidth; x++)
            {
                TileData tileData = currentMap.GetTile(x, y);
                string tileSymbol = GetTileSymbol(tileData);
                if (GUILayout.Button(tileSymbol, GUILayout.Width(30), GUILayout.Height(30)))
                {
                    //PlaceTile(x, y, selectedTileData);
                    HandleTileClick(x, y);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private void HandleTileClick(int x, int y)
    {
        // 선택된 타일 데이터로 타일 설정
        if (selectedTileData != null)
        {
            //currentMap.SetTile(x, y, selectedTileData);
            PlaceTile(x, y, selectedTileData);
            Repaint();
        }
    }

    private string GetTileSymbol(TileData tileData)
    {
        if (tileData == null || tileData.terrain == TileData.TerrainType.Empty) return "-";

        // tileName의 첫 글자를 대문자로 따와서 그리드에 표시함(추후 변경 가능)
        return tileData.TileName.Substring(0, 1).ToUpper(); 
    }

    // 에디터를 통한 타일 배치
    private void PlaceTile(int x, int y, TileData tileData)
    {
        currentMap.SetTile(x, y, tileData);

        // 현재 존재하는 타일 게임 오브젝트 제거
        Transform existingTile = currentMap.transform.Find($"Tile_{x}_{y}");
        if (existingTile != null)
        {
            DestroyImmediate(existingTile.gameObject);
        }

        if (tileData != null)
        {
            GameObject tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tiles/DefaultTile.prefab");
            if (tilePrefab != null && tileData.terrain != TileData.TerrainType.Empty)
            {
                GameObject tileInstance = PrefabUtility.InstantiatePrefab(tilePrefab) as GameObject;
                tileInstance.transform.SetParent(currentMap.transform);
                tileInstance.transform.localPosition = new Vector3(x, 0, mapHeight - 1 - y);
                tileInstance.name = $"Tile_{x}_{y}";
                
                Tile tileComponent = tileInstance.GetComponent<Tile>();
                if (tileComponent != null)
                {
                    tileComponent.SetTileData(tileData, new Vector2Int(x, y));
                }
            }
        }

        Repaint();
    }    

    // 맵 에디터에서 생성한 맵을 프리팹으로 저장한다.
    private void SaveMap()
    {
        string path = EditorUtility.SaveFilePanel("Save Map Prefab", MAP_PREFAB_PATH, "NewMap", "prefab");
        if (!string.IsNullOrEmpty(path))
        {
            path = FileUtil.GetProjectRelativePath(path);
            PrefabUtility.SaveAsPrefabAsset(currentMap.gameObject, path);
            AssetDatabase.Refresh();
        }
    }

    private void LoadMap()
    {
        string path = EditorUtility.OpenFilePanel("Select Map Prefab", MAP_PREFAB_PATH, "prefab");
        if (string.IsNullOrEmpty(path)) return;
        
        string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
        AssetDatabase.Refresh();
        GameObject loadedMapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);

        if (loadedMapPrefab == null)
        {
            Debug.LogError($"해당 경로의 맵 프리팹 불러오기 실패 : {relativePath}");
            return; 
        }

        RemoveExistingMaps();

        GameObject mapInstance = PrefabUtility.InstantiatePrefab(loadedMapPrefab) as GameObject;
        if (mapInstance == null)
        {
            Debug.LogError("Failed to instantiate map prefab.");
            return;
        }

        // 여기부터 currentMap은 로드한 맵이 된다.
        mapInstance.name = MAP_OBJECT_NAME; // 이름 통일
        currentMap = mapInstance.GetComponent<Map>();
        if (currentMap == null)
        {
            Debug.LogError("Instantiated prefab does not have a Map component.");
            DestroyImmediate(mapInstance);
            return;
        }

        mapWidth = currentMap.Width;
        mapHeight = currentMap.Height;

        // 기존 맵 로드
        currentMap.Initialize(mapWidth, mapHeight, true);

        // 에디터 UI 갱신
        Repaint();
        SceneView.RepaintAll(); 
    }

    // 기존 맵 제거하기
    private void RemoveExistingMaps()
    {
        Map[] existingMaps = FindObjectsOfType<Map>();
        foreach (Map map in existingMaps)
        {
            DestroyImmediate(map.gameObject);
        }
        currentMap = null; 
    }
    private void UpdateEditorGrid()
    {
        if (currentMap == null) return;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                TileData tileData = currentMap.GetTile(x, y);
                if (tileData != null)
                {
                    Debug.Log($"Updating grid at ({x}, {y}): {tileData.TileName}");
                    // 여기서 그리드의 각 셀을 업데이트합니다.
                    // 예를 들어, PlaceTile 메서드를 호출하거나 
                    // 또는 그리드 데이터 구조를 직접 업데이트할 수 있습니다.
                    PlaceTile(x, y, tileData);
                }
                else
                {
                    Debug.Log($"No tile data at ({x}, {y})");
                }
            }
        }
    }

    private void ResizeMap(int newWidth, int newHeight)
    {
        if (currentMap == null)
        {
            Debug.LogError("No map to resize");
            return;
        }

        // 새로운 크기의 타일 데이터 배열 생성
        TileData[,] newTiles = new TileData[newWidth, newHeight];

        // 기존 타일 데이터 복사
        int oldWidth = currentMap.Width;
        int oldHeight = currentMap.Height;
        for (int x = 0; x < Mathf.Min(oldWidth, newWidth); x++)
        {
            for (int y = 0; y < Mathf.Min(oldHeight, newHeight); y++)
            {
                newTiles[x, y] = currentMap.GetTile(x, y);
            }
        }

        // 새로 추가된 영역 초기화
        TileData emptyTile = availableTileData.Find(t => t.terrain == TileData.TerrainType.Empty);
        for (int x = 0; x < newWidth; x++)
        {
            for (int y = 0; y < newHeight; y++)
            {
                if (newTiles[x, y] == null)
                {
                    newTiles[x, y] = emptyTile;
                }
            }
        }

        // Map 컴포넌트 업데이트
        currentMap.Resize(newWidth, newHeight, newTiles);

        // 타일 게임 오브젝트 업데이트
        UpdateTileObjects();

        // 맵 크기 변수 업데이트
        mapWidth = newWidth;
        mapHeight = newHeight;

        // 씬 뷰와 에디터 윈도우 갱신
        SceneView.RepaintAll();
        Repaint();
    }

    private void UpdateTileObjects()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                TileData tileData = currentMap.GetTile(x, y);
                Transform existingTile = currentMap.transform.Find($"Tile_{x}_{y}");

                if (existingTile != null)
                {
                    // 기존 타일 업데이트
                    Tile tileComponent = existingTile.GetComponent<Tile>();
                    if (tileComponent != null)
                    {
                        tileComponent.SetTileData(tileData, new Vector2Int(x, y));
                    }
                    existingTile.localPosition = new Vector3(x, 0, mapHeight - 1 - y);
                }
                else if (tileData != null && tileData.terrain != TileData.TerrainType.Empty)
                {
                    // 새 타일 생성
                    PlaceTile(x, y, tileData);
                }
            }
        }

        // 맵 크기를 벗어난 타일 제거
        foreach (Transform child in currentMap.transform)
        {
            Tile tile = child.GetComponent<Tile>();
            if (tile != null)
            {
                Vector2Int pos = tile.GridPosition;
                if (pos.x >= mapWidth || pos.y >= mapHeight)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
