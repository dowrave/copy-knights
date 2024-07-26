using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

public class MapEditorWindow : EditorWindow
{
    public int mapWidth = 5;
    public int mapHeight = 5;
    private Map currentMap;
    private TileData selectedTileData;
    private Vector2 scrollPosition;
    private List<TileData> availableTileData = new List<TileData>();

    private GameObject loadedMapPrefab;
    private List<Vector2Int> startTilePositions = new List<Vector2Int>();

    private const string MAP_OBJECT_NAME = "EditorMap"; 
    private const string MAP_PREFAB_PATH = "Assets/Prefabs/Map";

    //[SerializeField] private GameObject spawnerPrefab; // �ν����Ϳ��� �Ҵ�
    private GameObject spawnerPrefab;

    [MenuItem("Window/Map Editor")]
    public static void ShowWindow()
    {
        GetWindow<MapEditorWindow>("Map Editor");
    }

    // ������ â�� ��� �� �����
    private void OnEnable()
    {
        LoadAvailableTileData();
        LoadExistingMap();
        LoadSpawnerPrefab();
    }

    // ���̾��Ű�� �ִ� �� ������Ʈ�� ������ �����´�
    private void LoadExistingMap()
    {
        currentMap = FindObjectOfType<Map>(); // "������Ʈ"�� ã�� �޼�����!!
        //CheckMapInHierarchy()

        if (currentMap != null)
        {
            mapWidth = currentMap.Width;
            mapHeight = currentMap.Height;

            // ���� �� �ε�
            currentMap.Initialize(mapWidth, mapHeight, true);
            UpdateExistingTiles();

            // �� ������Ʈ�� ũ�� ����
            currentMap.transform.localScale = Vector3.one;

            // ������ UI ����
            Repaint();
            SceneView.RepaintAll();
        }
        //}
    }

    private void UpdateExistingTiles()
    {
        if (currentMap == null) return;

        foreach (Transform tileTransform in currentMap.transform)
        {
            Tile tileComponent = tileTransform.GetComponent<Tile>();
            if (tileComponent != null)
            {
                tileComponent.AdjustCubeScale();
            }
        }
    }

    //private void OnDisable()
    //{
    //    if (currentMap != null)
    //    {
    //        DestroyImmediate(currentMap.gameObject);
    //    }
    //}


    private bool CheckMapInHierarchy()
    {
        Map currentMap = FindObjectOfType<Map>();
        if (currentMap) return true;
        return false;
    }


    // ������ ScriptableObject�� ������ tileData���� �ҷ����� ����
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

    // ������ �ڵ� �Ҵ��ϱ�
    private void LoadSpawnerPrefab()
    {
        spawnerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy Spawner.prefab");
        if (spawnerPrefab == null)
        {
            Debug.LogError("�ʿ� �������� �Ҵ���� ���� ����");
        }
    }


    private void InitializeNewMap()
    {
        // ���� �� ���� 
        RemoveExistingMaps();

        // �� �� ���� ������Ʈ ����
        GameObject mapObject = new GameObject(MAP_OBJECT_NAME);
        currentMap = mapObject.AddComponent<Map>();

        // Spawner ������ Map�� �Ҵ�
        if (spawnerPrefab != null)
        {
            currentMap.SetEnemySpawnerPrefab(spawnerPrefab);
        }
        else
        {
            Debug.LogWarning("�ʿ� ������ �Ҵ� �� : �����տ� �ε���� �ʾ���");
        }

        // �� �� �ʱ�ȭ(load = false)
        currentMap.Initialize(mapWidth, mapHeight, false);

        // ������ UI ����
        Repaint();
        SceneView.RepaintAll();
    }


    private void OnGUI()
    {
        if (currentMap == null)
        {
            LoadExistingMap();
        }
        
        if (!CheckMapInHierarchy())
        { 
            GUILayout.Label("���� �����ϴ�. ���ο� ���� �����ϰų� �ε��ϼ���.", EditorStyles.boldLabel);
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

        // �� ũ�� ���� �� ���� �ʿ��� ������ �߻�
        if (newWidth != mapWidth || newHeight != mapHeight)
        {
            ResizeMap(newWidth, newHeight);
        }

        selectedTileData = (TileData)EditorGUILayout.ObjectField("Selected Tile", selectedTileData, typeof(TileData), false);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // �� �׸��� �׸���
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
        if (currentMap == null)
        {
            Debug.LogError("���� �� ������ ����");
            return;
        }

        for (int y = 0; y < mapHeight; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < mapWidth; x++)
            {
                TileData tileData = currentMap.GetTile(x, y);
                
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
        // ���õ� Ÿ�� �����ͷ� Ÿ�� ����
        if (selectedTileData != null)
        {
            PlaceTile(x, y, selectedTileData);
            Repaint();
        }
    }


    private string GetTileSymbol(TileData tileData)
    {
        if (tileData == null || tileData.terrain == TileData.TerrainType.Empty) return "-";

        // tileName�� ù ���ڸ� �빮�ڷ� ���ͼ� �׸��忡 ǥ����(���� ���� ����)
        return tileData.TileName.Substring(0, 1).ToUpper(); 
    }
    

    // �����͸� ���� Ÿ�� ���� �� ��ġ
    private void PlaceTile(int x, int y, TileData tileData)
    {
        currentMap.SetTile(x, y, tileData);

        // ���� �����ϴ� Ÿ�� ���� ������Ʈ ����
        Transform existingTile = currentMap.transform.Find($"Tile_{x}_{y}");
        if (existingTile != null)
        {
            DestroyImmediate(existingTile.gameObject);
        }

        // Ÿ�� ��ġ ó��
        if (tileData != null && tileData.terrain != TileData.TerrainType.Empty)
        {
            GameObject tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tiles/DefaultTile.prefab");
            if (tilePrefab != null && tileData.terrain != TileData.TerrainType.Empty)
            {
                GameObject tileInstance = PrefabUtility.InstantiatePrefab(tilePrefab) as GameObject;
                tileInstance.transform.SetParent(currentMap.transform);

                // �����쿡 ���̴� ��� Ÿ���� ��ġ�ϱ� ���� mapHeight - 1 - y ������ z���� �����ȴ�.
                tileInstance.transform.localPosition = new Vector3(x, 0, mapHeight - 1 - y); 

                // Ÿ�� �̸� ����
                //string tileName = $"Tile_{x}_{y}";
                string tileName = $"Tile";
                if (tileData.isStartPoint)
                {
                    tileName += "_Start";
                }
                else if (tileData.isEndPoint)
                {
                    tileName += "_End";
                }
                tileInstance.name = tileName;


                Tile tileComponent = tileInstance.GetComponent<Tile>();
                if (tileComponent != null)
                {
                    tileComponent.SetTileData(tileData, new Vector2Int(x, y)); // ���⼭ �ش� Ÿ�Ͽ� gridPosition�� ����ȴ�.
                }

                if (tileData.isStartPoint)
                {
                    Debug.Log("Start Ÿ�� ���� Ŭ��");
                    CreateSpawner(x, y, tileInstance);
                }
                else if (startTilePositions.Contains(new Vector2Int(x, y)))
                {
                    // Start Ÿ�� ��ġ�� �ٸ� Ÿ���� ��ġ�ϴ� ���
                    RemoveSpawner(new Vector2Int(x, y));
                }
            }
        }

        Repaint();
    }    


    private void CreateSpawner(int x, int y, GameObject tileInstance)
    {

        if (spawnerPrefab != null)
        {
            Vector2Int pos = new Vector2Int(x, y);
            if (!startTilePositions.Contains(pos))
            {
                startTilePositions.Add(pos);
            }

            // �ڽ� ������Ʈ�� Spawner ����
            GameObject spawnerObject = PrefabUtility.InstantiatePrefab(spawnerPrefab) as GameObject;
            spawnerObject.transform.SetParent(tileInstance.transform);
            spawnerObject.transform.localPosition = Vector3.zero;
            spawnerObject.name = "EnemySpawner";
            Debug.Log("Start Ÿ�� ���� �� ������ �ڽ� ������Ʈ�� ����");

            EnemySpawner spawner = spawnerObject.GetComponent<EnemySpawner>();
            if (spawner == null)
            {
                spawner = spawnerObject.AddComponent<EnemySpawner>();
            }
        }
    }


    private void RemoveSpawner(Vector2Int pos)
    {
        startTilePositions.Remove(pos);
        Transform startTile = currentMap.transform.Find($"Tile_{pos.x}_{pos.y}");

        if (startTile != null)
        {
            EnemySpawner spawner = startTile.GetComponent<EnemySpawner>();
            if (spawner != null)
            {
                DestroyImmediate(spawner);
            }
        }
    }


    // �� �����Ϳ��� ������ ���� ���������� �����Ѵ�.
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
            Debug.LogError($"�ش� ����� �� ������ �ҷ����� ���� : {relativePath}");
            return; 
        }

        RemoveExistingMaps();

        GameObject mapInstance = PrefabUtility.InstantiatePrefab(loadedMapPrefab) as GameObject;
        if (mapInstance == null)
        {
            Debug.LogError("Failed to instantiate map prefab.");
            return;
        }

        // ������� currentMap�� �ε��� ���� �ȴ�.
        mapInstance.name = MAP_OBJECT_NAME; // �̸� ����
        currentMap = mapInstance.GetComponent<Map>();
        if (currentMap == null)
        {
            Debug.LogError("Instantiated prefab does not have a Map component.");
            DestroyImmediate(mapInstance);
            return;
        }

        mapWidth = currentMap.Width;
        mapHeight = currentMap.Height;

        if (spawnerPrefab != null)
        {
            currentMap.SetEnemySpawnerPrefab(spawnerPrefab);
        }
        else
        {
            Debug.LogWarning("�� �ҷ����� �� : �� ������ �������� �Ҵ���� ����");
        }

        // ���� �� �ε�
        currentMap.Initialize(mapWidth, mapHeight, true);

        // ������ UI ����
        Repaint();
        SceneView.RepaintAll(); 
    }


    // ���� �� �����ϱ�
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
        if (currentMap == null)
        {
            Debug.LogError("No map to resize");
            return;
        }

        // ���ο� ũ���� Ÿ�� ������ �迭 ����
        TileData[,] newTiles = new TileData[newWidth, newHeight];

        // ���� Ÿ�� ������ ����
        int oldWidth = currentMap.Width;
        int oldHeight = currentMap.Height;
        for (int x = 0; x < Mathf.Min(oldWidth, newWidth); x++)
        {
            for (int y = 0; y < Mathf.Min(oldHeight, newHeight); y++)
            {
                newTiles[x, y] = currentMap.GetTile(x, y);
            }
        }

        // ���� �߰��� ���� �ʱ�ȭ
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

        // Map ������Ʈ ������Ʈ
        currentMap.Resize(newWidth, newHeight, newTiles);

        // Ÿ�� ���� ������Ʈ ������Ʈ
        UpdateTileObjects();

        // �� ũ�� ���� ������Ʈ
        mapWidth = newWidth;
        mapHeight = newHeight;

        // �� ��� ������ ������ ����
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
                    // ���� Ÿ�� ������Ʈ
                    Tile tileComponent = existingTile.GetComponent<Tile>();
                    if (tileComponent != null)
                    {
                        tileComponent.SetTileData(tileData, new Vector2Int(x, y));
                    }
                    existingTile.localPosition = new Vector3(x, 0, mapHeight - 1 - y);
                    existingTile.localScale = new Vector3(0.98f, existingTile.localScale.y, 0.98f);
                }
                else if (tileData != null && tileData.terrain != TileData.TerrainType.Empty)
                {
                    // �� Ÿ�� ����
                    PlaceTile(x, y, tileData);
                }
            }
        }

        // �� ũ�⸦ ��� Ÿ�� ����
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

    // �׸��� ��ǥ�踦 �����ϰ�, �ʿ��� ���� ��ȯ ������ �����Ѵ�.
    private Vector3 GridToWorldPosition(int x, int y)
    {
        return new Vector3(x, 0, mapHeight - 1 - y);
    }
}
