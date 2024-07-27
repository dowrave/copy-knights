using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MapEditorWindow : EditorWindow
{
    public int mapWidth = 5;
    public int mapHeight = 5;
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
        LoadAvailableTileData();
        LoadSpawnerPrefab();
        LoadTilePrefab();
        InitializeNewMap();
    }

    private void OnGUI()
    {
        GUILayout.Label("Map Editor", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        int newWidth = EditorGUILayout.IntField("Width", mapWidth);
        int newHeight = EditorGUILayout.IntField("Height", mapHeight);
        EditorGUILayout.EndHorizontal();

        if (newWidth != mapWidth || newHeight != mapHeight)
        {
            ResizeMap(newWidth, newHeight);
        }

        selectedTileData = (TileData)EditorGUILayout.ObjectField("Selected Tile", selectedTileData, typeof(TileData), false);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawMapGrid();

        EditorGUILayout.EndScrollView();

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
        if (currentMap == null) return;

        for (int y = 0; y < mapHeight; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < mapWidth; x++)
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

    private void InitializeNewMap()
    {

        RemoveExistingMaps();

        GameObject stageObject = GetStageObject();
        GameObject mapObject = new GameObject(MAP_OBJECT_NAME);
        mapObject.transform.SetParent(stageObject.transform);

        currentMap = mapObject.AddComponent<Map>();
        currentMap.Initialize(mapWidth, mapHeight, false);

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
    }

    private void LoadMap()
    {
        string path = EditorUtility.OpenFilePanel("Select Map Prefab", MAP_PREFAB_PATH, "prefab");
        if (string.IsNullOrEmpty(path)) return;

        string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
        GameObject loadedMapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);

        if (loadedMapPrefab == null)
        {
            Debug.LogError($"Failed to load map prefab from path: {relativePath}");
            return;
        }

        RemoveExistingMaps();

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

        SerializedObject serializedMap = new SerializedObject(currentMap);
        SerializedProperty tilePrefabProperty = serializedMap.FindProperty("tilePrefab");
        if (tilePrefabProperty.objectReferenceValue == null)
        {
            tilePrefabProperty.objectReferenceValue = tilePrefab;
            serializedMap.ApplyModifiedProperties();
        }

        currentMap.Initialize(currentMap.Width, currentMap.Height, true);
        mapWidth = currentMap.Width;
        mapHeight = currentMap.Height;

        DrawMapGrid();

        SceneView.RepaintAll();
        Repaint();
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

        currentMap.Initialize(newWidth, newHeight, false);
        mapWidth = newWidth;
        mapHeight = newHeight;

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