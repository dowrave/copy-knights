using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public class MapEditorWindow : EditorWindow
{
    public int currentMapWidth;
    public int currentMapHeight;
    private int newMapWidth;
    private int newMapHeight;
    private Map currentMap;
    private TileData selectedTileData;
    private Vector2 scrollPosition;
    private List<TileData> availableTileData = new List<TileData>();

    private GameObject spawnerPrefab;
    private GameObject tilePrefab;

    private const string MAP_OBJECT_NAME = "Map";
    private const string MAP_PREFAB_PATH = "Assets/Prefabs/Map";

    [MenuItem("Window/Map Editor")]
    public static void ShowWindow()
    {
        GetWindow<MapEditorWindow>("Map Editor");
    }

    private void OnEnable()
    {
        if (currentMap != null)
        {
            currentMapWidth = currentMap.Width;
            currentMapHeight = currentMap.Height;
        }

        else
        {
            currentMapWidth = 5;
            currentMapHeight = 5;
        }

        newMapWidth = currentMapWidth;
        newMapHeight = currentMapHeight;

        LoadAvailableTileData(); // ��� ������ Ÿ�� ���� �غ�
        LoadSpawnerPrefab(); // ������ �غ�
        LoadTilePrefab(); // Ÿ�� ���� �غ�
        FindExistingMap();
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
    
    // �����츦 ������ �� ���̾��Ű�� ���� ���� ��� ǥ�õǴ� ȭ��
    private void DrawNoMapUI()
    {
        EditorGUILayout.HelpBox("���� ���� �����ϴ�. �ҷ����ų� ���� ���弼��", MessageType.Info);
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
        // �ʺ�, ���� ����â
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

        EditorGUILayout.Space(10); // 10 �ȼ��� ���� �߰�

        // Ÿ�� ����â
        selectedTileData = (TileData)EditorGUILayout.ObjectField("Selected Tile", selectedTileData, typeof(TileData), false);

        // �� �׸��� ��ġ
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawMapGrid(currentMapWidth, currentMapHeight);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset Map"))
        {
            if (EditorUtility.DisplayDialog("Reset Map",
                "���� �ʱ�ȭ�Ͻðڽ��ϱ�?",
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

    private void DrawMapGrid(int width, int height)
    {
        if (currentMap == null) return;

        for (int y = 0; y < height; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < width; x++)
            {
                TileData tileData = currentMap.GetTileData(x, y);
                string tileSymbol = GetTileSymbol(tileData);
                if (GUILayout.Button(tileSymbol, GUILayout.Width(30), GUILayout.Height(30)))
                {
                    HandleTileClick(x, y);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void HandleTileClick(int x, int y)
    {
        if (selectedTileData != null)
        {
            currentMap.SetTile(x, y, selectedTileData);
            SceneView.RepaintAll();
            Repaint();
        }
    }

    // ��Ʈ ������Ʈ�� �ִ� �������� ������Ʈ�� ã�´�
    private GameObject GetStageObject()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject go in rootObjects)
        {
            if (go.name.StartsWith("Stage"))
            {
                return go;
            }
        }
        return null;
    }

    // ���� ���� �ִٸ� �ε��ϰ�, ���ٸ� ���ο� ���� �ʱ�ȭ�Ѵ�
    private void FindExistingMap()
    {
        GameObject mapObject = GameObject.Find(MAP_OBJECT_NAME);
        if (mapObject != null)
        {
            currentMap = mapObject.GetComponent<Map>();
            if (currentMap != null)
            {
                currentMapWidth = currentMap.Width;
                currentMapHeight = currentMap.Height;
            }
        }
    }

    private void InitializeNewMap()
    {

        RemoveExistingMaps();

        // Stage ������Ʈ �Ʒ��� Map ������Ʈ �Ҵ�
        GameObject stageObject = GetStageObject();
        GameObject mapObject = new GameObject(MAP_OBJECT_NAME);
        mapObject.transform.SetParent(stageObject.transform);
        currentMap = mapObject.AddComponent<Map>();

        // Map ������Ʈ �ʱ�ȭ(load = false)
        currentMap.Initialize(currentMapWidth, currentMapHeight, false);

        // Map ������Ʈ�� tilePrefab �Ӽ��� ������ ��ũ��Ʈ���� ����, �� ���� �� �ùٸ� Ÿ�� �������� ���ǵ��� �Ѵ�.
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


    private string GetTileSymbol(TileData tileData)
    {
        if (tileData == null || tileData.terrain == TileData.TerrainType.Empty) return "-";
        return tileData.TileName.Substring(0, 1).ToUpper();
    }


    private void SaveMap()
    {
        if (currentMap == null) return;

        string path = EditorUtility.SaveFilePanel("Save Map Prefab", MAP_PREFAB_PATH, "NewMap", "prefab");
        if (!string.IsNullOrEmpty(path))
        {
            path = FileUtil.GetProjectRelativePath(path);
            PrefabUtility.SaveAsPrefabAsset(currentMap.gameObject, path);
            AssetDatabase.Refresh();
        }
        Debug.LogWarning(currentMap.GetTileDataDebugString());
    }


    private void LoadMap()
    {
        // �� ������ ��� ���� �� �ε� 
        string path = EditorUtility.OpenFilePanel("Select Map Prefab", MAP_PREFAB_PATH, "prefab");
        if (string.IsNullOrEmpty(path)) return;
        string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
        GameObject loadedMapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
        if (loadedMapPrefab == null)
        {
            Debug.LogError($"Failed to load map prefab from path: {relativePath}");
            return;
        }

        // ���� ���� �����ϴ� �� ���� 
        RemoveExistingMaps();

        // �������� ������Ʈ �Ʒ��� �� ������Ʈ ����
        GameObject stageObject = GetStageObject();
        GameObject mapInstance = PrefabUtility.InstantiatePrefab(loadedMapPrefab, stageObject.transform) as GameObject;
        mapInstance.name = MAP_OBJECT_NAME;
        currentMap = mapInstance.GetComponent<Map>();
        if (currentMap == null)
        {
            Debug.LogError("Loaded prefab does not have a Map component.");
            DestroyImmediate(mapInstance);
            return;
        }

        // Map ������Ʈ�� tilePrefab �Ӽ��� ������ ��ũ��Ʈ���� ����, �� ���� �� �ùٸ� Ÿ�� �������� ���ǵ��� �Ѵ�.
        SerializedObject serializedMap = new SerializedObject(currentMap);
        SerializedProperty tilePrefabProperty = serializedMap.FindProperty("tilePrefab");
        if (tilePrefabProperty.objectReferenceValue == null)
        {
            tilePrefabProperty.objectReferenceValue = tilePrefab;
            serializedMap.ApplyModifiedProperties();
        }

        currentMap.Initialize(currentMap.Width, currentMap.Height, true);

        currentMapWidth = currentMap.Width;
        currentMapHeight = currentMap.Height;

        SceneView.RepaintAll(); // ���� ������Ʈ
        Repaint(); // �����츦 ������Ʈ
    }


    private void RemoveExistingMaps()
    {
        Map[] existingMaps = FindObjectsOfType<Map>();
        foreach (Map map in existingMaps)
        {
            DestroyImmediate(map.gameObject);
        }
        currentMap = null;
    }


    private void ResizeMap(int newWidth, int newHeight)
    {
        if (currentMap == null) return;

        // ���� �� Ÿ�� ������ �ӽ� ����
        TileData[,] oldTileData = new TileData[currentMapWidth, currentMapHeight];

        for (int x = 0; x < currentMapWidth; x++)
        {
            for (int y = 0; y < currentMapHeight; y++)
            {
                oldTileData[x, y] = currentMap.GetTileData(x, y);
            }
        }

        currentMap.RemoveAllTiles();

        // �� ũ�� ����
        currentMap.Initialize(newWidth, newHeight, false);

        // ���� ������ ����
        for (int x = 0; x < Mathf.Min(currentMapWidth, newWidth); x++)
        {
            for (int y = 0; y < Mathf.Min(currentMapHeight, newHeight); y++)
            {
                if (oldTileData[x, y] != null)
                {
                    // �� ��ġ�� Ÿ�� ������ ����
                    TileData newTileData = oldTileData[x, y];
                    currentMap.SetTile(x, y, newTileData);
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
        if (availableTileData.Count > 0)
        {
            selectedTileData = availableTileData[0];
        }
    }


    private void LoadSpawnerPrefab()
    {
        spawnerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy Spawner.prefab");
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