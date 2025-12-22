using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public class MapEditorWindow : EditorWindow
{

    private GameObject? spawnerPrefab;

    public int currentMapWidth = 5;
    public int currentMapHeight = 5;
    private int newMapWidth;
    private int newMapHeight;
    private Map? currentMap;
    private TileData? selectedTileData;
    private Vector2 scrollPosition;
    private List<TileData> availableTileData = new List<TileData>();

    private GameObject? tilePrefab;

    private const string MAP_OBJECT_NAME = "Map";
    private const string MAP_PREFAB_PATH = "Assets/Prefabs/Map";

    [MenuItem("Window/Map Editor")]
    public static void ShowWindow()
    {
        GetWindow<MapEditorWindow>("Map Editor");
    }

    private void OnEnable()
    {
        LoadAvailableTileData(); // 사용 가능한 타일 정보 준비
        LoadSpawnerPrefab(); // 스포너 준비
        LoadTilePrefab(); // 타일 정보 준비
        FindExistingMap();

        // 윈도우에 나타나는 Width, Height 초기화
        newMapWidth = currentMapWidth;
        newMapHeight = currentMapHeight;
    }

    private void OnGUI()
    {
        GUILayout.Label("Map Editor", EditorStyles.boldLabel);

        if (currentMap == null)
        {
            DrawNoMapUI();
        }
        else
        {
            DrawMapUI();
        }
    }
    
    // 윈도우를 열었을 때 하이어라키에 맵이 없는 경우 표시되는 화면
    private void DrawNoMapUI()
    {
        EditorGUILayout.HelpBox("씬에 맵이 없습니다. 불러오거나 새로 만드세용", MessageType.Info);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create New Map"))
        {
            InitializeNewMap();
        }
        if (GUILayout.Button("Load Map"))
        {
            LoadMap();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawMapUI()
    {
        // 너비, 높이 수정창
        EditorGUILayout.BeginHorizontal();
        newMapWidth = EditorGUILayout.IntField("Width", newMapWidth);
        newMapHeight = EditorGUILayout.IntField("Height", newMapHeight);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Resize Map"))
        {
            if (EditorUtility.DisplayDialog("Resize Map",
                $"Are you sure you want to resize the map to {newMapWidth}x{newMapHeight}?",
                "Resize", "Cancel"))
            {
                ResizeMap(newMapWidth, newMapHeight);
            }
        }

        EditorGUILayout.Space(10); // 10 픽셀의 간격 추가

        // 타일 선택창
        selectedTileData = (TileData)EditorGUILayout.ObjectField("Selected Tile", selectedTileData, typeof(TileData), false);

        // 맵 그리드 위치
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawMapGrid(currentMapWidth, currentMapHeight);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset Map"))
        {
            if (EditorUtility.DisplayDialog("Reset Map",
                "맵을 초기화하시겠습니까?",
                "Reset", "Cancel"))
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
        EditorGUILayout.EndHorizontal();
    }

    // 수정된 DrawMapGrid 메서드
    private void DrawMapGrid(int width, int height)
    {
        if (currentMap == null) return;

        for (int y = 0; y < height; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < width; x++)
            {
                // 타일 데이터가 null이어도 GetTileSymbol 내에서 '-'를 반환하도록 수정합니다.
                TileData? tileData = currentMap.GetTileData(x, y);
                string tileSymbol = GetTileSymbol(tileData);
                if (GUILayout.Button(tileSymbol, GUILayout.Width(30), GUILayout.Height(30)))
                {
                    HandleTileClick(x, y);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 에디터 상에서 타일 배치
    /// </summary>
    private void HandleTileClick(int x, int y)
    {
        if (selectedTileData != null && currentMap != null)
        {
            currentMap.UpdateTile(x, y, selectedTileData);
            SceneView.RepaintAll();
            Repaint();
        }
    }

    // 현재 하이어라키에 있는 Map 컴포넌트를 찾음
    private void FindExistingMap()
    {
        currentMap = FindFirstObjectByType<Map>();
        if (currentMap != null)
        {
            currentMapWidth = currentMap.Width;
            currentMapHeight = currentMap.Height;
            // 맵 다시 초기화 - 컴파일이 다시 된 다음에 MapEditorWindow에 참조를 유실하는 문제가 있음
            currentMap.InitializeOnEditor(currentMapWidth, currentMapHeight, true);
        }
    }

    private void InitializeNewMap()
    {

        RemoveExistingMaps();

        // Stage 오브젝트 아래에 Map 오브젝트 할당
        GameObject mapObject = new GameObject(MAP_OBJECT_NAME);
        currentMap = mapObject.AddComponent<Map>();

        // Map 오브젝트 초기화(load = false)
        currentMap.InitializeOnEditor(currentMapWidth, currentMapHeight, false);

        // Map 컴포넌트의 tilePrefab 속성을 에디터 스크립트에서 설정, 맵 생성 시 올바른 타일 프리팹이 사용되도록 한다.
        SerializedObject serializedMap = new SerializedObject(currentMap);
        SerializedProperty tilePrefabProperty = serializedMap.FindProperty("tilePrefab");
        tilePrefabProperty.objectReferenceValue = tilePrefab;
        serializedMap.ApplyModifiedProperties();


        if (spawnerPrefab != null)
        {
            currentMap.SetEnemySpawnerPrefab(spawnerPrefab);
        }

        SceneView.RepaintAll();
        Repaint();
    }


    // GetTileSymbol 메서드의 파라미터를 nullable로 변경하여 null 체크를 수행하게 합니다.
    private string GetTileSymbol(TileData? tileData)
    {
        if (tileData == null || tileData.Terrain == TileData.TerrainType.Empty)
            return "-";
        return tileData.TileName.Substring(0, 1).ToUpper();
    }


    private void SaveMap()
    {
        if (currentMap == null) return;

        string path = EditorUtility.SaveFilePanel("Save Map Prefab", MAP_PREFAB_PATH, "NewMap", "prefab");
        if (!string.IsNullOrEmpty(path))
        {
            path = FileUtil.GetProjectRelativePath(path);
            //PrefabUtility.SaveAsPrefabAsset(currentMap.gameObject, path); // Map을 단순히 저장만 함(프리팹과 연결 X)
            PrefabUtility.SaveAsPrefabAssetAndConnect(currentMap.gameObject, path, InteractionMode.UserAction); // 프리팹과 연결까지 수행
            AssetDatabase.Refresh();
        }
        Debug.LogWarning(currentMap.GetTileDataDebugString());
    }


    private void LoadMap()
    {
        // 맵 프리팹 경로 추적 및 로드 
        string path = EditorUtility.OpenFilePanel("Select Map Prefab", MAP_PREFAB_PATH, "prefab");
        if (string.IsNullOrEmpty(path)) return;
        string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
        GameObject loadedMapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
        if (loadedMapPrefab == null)
        {
            Debug.LogError($"Failed to load map prefab from path: {relativePath}");
            return;
        }

        // 현재 씬에 존재하는 맵 제거 
        RemoveExistingMaps();

        // 맵에 관한 설정
        GameObject? mapInstance = PrefabUtility.InstantiatePrefab(loadedMapPrefab) as GameObject;
        if (mapInstance != null)
        {
            mapInstance.name = MAP_OBJECT_NAME;
            currentMap = mapInstance.GetComponent<Map>();
            if (currentMap == null)
            {
                Debug.LogError("Loaded prefab does not have a Map component.");
                DestroyImmediate(mapInstance);
                return;
            }

            currentMap.InitializeOnEditor(currentMap.Width, currentMap.Height, true);
            currentMapWidth = currentMap.Width;
            currentMapHeight = currentMap.Height;

            SceneView.RepaintAll(); // 씬을 리페인트
            Repaint(); // 윈도우를 리페인트
        }
    }


    private void RemoveExistingMaps()
    {
        Map[] existingMaps = FindObjectsByType<Map>(FindObjectsSortMode.None);
        foreach (Map map in existingMaps)
        {
            DestroyImmediate(map.gameObject);
        }
        currentMap = null;
    }


    private void ResizeMap(int newWidth, int newHeight)
    {
        if (currentMap == null) return;

        // 현재 맵 타일 데이터 임시 저장
        TileData[,] oldTileData = new TileData[currentMapWidth, currentMapHeight];

        for (int x = 0; x < currentMapWidth; x++)
        {
            for (int y = 0; y < currentMapHeight; y++)
            {
                TileData? tileData = currentMap.GetTileData(x, y);
                if (tileData != null)
                {
                    oldTileData[x, y] = tileData;
                }
            }
        }

        currentMap.RemoveAllTiles();

        // 맵 크기 조정
        currentMap.InitializeOnEditor(newWidth, newHeight, false);

        // 기존 데이터 복원
        for (int x = 0; x < Mathf.Min(currentMapWidth, newWidth); x++)
        {
            for (int y = 0; y < Mathf.Min(currentMapHeight, newHeight); y++)
            {
                if (oldTileData[x, y] != null)
                {
                    // 새 위치에 타일 데이터 설정
                    TileData newTileData = oldTileData[x, y];
                    currentMap.UpdateTile(x, y, newTileData);
                }
            }
        }

        currentMapWidth = newWidth;
        currentMapHeight = newHeight;

        SceneView.RepaintAll();
        Repaint();
    }


    private void LoadAvailableTileData()
    {
        availableTileData.Clear();
        string[] guids = AssetDatabase.FindAssets("t:TileData", new[] { "Assets/ScriptableObjects/TileData" });
       
        foreach (string guid in guids)
        {
            Debug.Log(guid);
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TileData tileData = AssetDatabase.LoadAssetAtPath<TileData>(path);
            if (tileData != null)
            {
                availableTileData.Add(tileData);
            }
        }
        if (availableTileData.Count > 0)
        {
            selectedTileData = availableTileData[0];
        }
    }


    private void LoadSpawnerPrefab()
    {
        spawnerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemySpawner.prefab");
        if (spawnerPrefab == null)
        {
            Debug.LogWarning("Enemy spawner prefab not found");
        }
    }


    private void LoadTilePrefab()
    {
        tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tiles/DefaultTile.prefab");
        if (tilePrefab == null)
        {
            Debug.LogError("Default tile prefab not found");
        }
    }
}