using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MapEditorWindow : EditorWindow
{
    public int mapWidth = 5;
    public int mapHeight = 5;
    private TileData[,] map;
    private TileData selectedTileData;
    private Vector2 scrollPosition;
    private GameObject previewContainer;
    private Dictionary<Vector2Int, GameObject> previewTiles = new Dictionary<Vector2Int, GameObject>();
    private List<TileData> availableTileData = new List<TileData>();

    private GameObject loadedMapPrefab;

    private const string MAP_PREFAB_PATH = "Assets/Prefabs/Map";

    [MenuItem("Window/Map Editor")]
    public static void ShowWindow()
    {
        GetWindow<MapEditorWindow>("Map Editor");
    }

    private void OnEnable()
    {
        LoadAvailableTileData();
        InitializeMap();
        CreatePreviewContainer();
    }

    private void OnDisable()
    {
        DestroyPreviewContainer();
    }

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

    private void InitializeMap()
    {
        map = new TileData[mapWidth, mapHeight];
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                map[x, y] = availableTileData.Find(t => t.terrain == TileData.TerrainType.Empty); // 초기화 측면에서도 Empty는 필요해 보인다
            }
        }
    }
    
    private void CreatePreviewContainer()
    {
        previewContainer = new GameObject("Map Preview");
        previewContainer.hideFlags = HideFlags.HideAndDontSave;
    }

    private void DestroyPreviewContainer()
    {
        if (previewContainer != null)
        {
            DestroyImmediate(previewContainer);
        }
        previewTiles.Clear();
    }

    private void OnGUI()
    {
        GUILayout.Label("Map Editor", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        mapWidth = EditorGUILayout.IntField("Width", mapWidth);
        mapHeight = EditorGUILayout.IntField("Height", mapHeight);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Reset Map"))
        {
            InitializeMap();
            UpdatePreview(); 
        }

        selectedTileData = (TileData)EditorGUILayout.ObjectField("Selected Tile", selectedTileData, typeof(TileData), false);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // 맵 그리드 그리기
        //for (int y = 0; y < mapHeight - 1; y++)
        for (int y = mapHeight - 1; y >= 0; y--) // 화면에 보이는 대로 그리드 표시
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < mapWidth; x++)
            {
                if (GUILayout.Button(GetTileSymbol(map[x, y]), GUILayout.Width(30), GUILayout.Height(30)))
                {
                    map[x, y] = selectedTileData;
                    UpdatePreviewTile(x, y);
                }
            }
            EditorGUILayout.EndHorizontal();    
        }
        EditorGUILayout.EndScrollView();

        //if (GUILayout.Button("Load Map"))
        //{
        //    LoadMap();
        //}

        if (GUILayout.Button("Update 3D Preview"))
        {
            UpdatePreview();
        }
    
        if (GUILayout.Button("Save Map")) 
        {
            SaveMap();
        }
    }

    private string GetTileSymbol(TileData tileData)
    {
        if (tileData == null || tileData.terrain == TileData.TerrainType.Empty) return "-";

        // tileName의 첫 글자를 대문자로 따와서 그리드에 표시함(추후 변경 가능)
        return tileData.TileName.Substring(0, 1).ToUpper(); 
    }

    // 맵 에디터에서 생성한 맵을 프리팹으로 저장한다.
    private void SaveMap()
    {
        GameObject mapObject = new GameObject("Map");

        for (int x=0; x < mapWidth; x ++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                TileData tileData = map[x, y];
                if (tileData != null && tileData.terrain != TileData.TerrainType.Empty)
                {
                    GameObject tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tiles/DefaultTile.prefab");
                    if (tilePrefab != null)
                    {
                        GameObject tileInstance = PrefabUtility.InstantiatePrefab(tilePrefab) as GameObject;
                        tileInstance.transform.SetParent(mapObject.transform);
                        tileInstance.transform.localPosition = new Vector3(x, 0, mapHeight - 1 - y);

                        Tile tileComponent = tileInstance.GetComponent<Tile>();
                        if (tileComponent != null)
                        {
                            tileComponent.SetTileData(tileData, new Vector2Int(x, y));
                        } 
                    }
                }
            }
        }

        string path = EditorUtility.SaveFilePanel("Save Map Prefab", MAP_PREFAB_PATH, "NewMap", "prefab");
        if (!string.IsNullOrEmpty(path))
        {
            path = FileUtil.GetProjectRelativePath(path);
            PrefabUtility.SaveAsPrefabAsset(mapObject, path);
            DestroyImmediate(mapObject);
            AssetDatabase.Refresh();
        }
    }

    private void UpdatePreview()
    {
        foreach (var kvp in previewTiles)
        {
            DestroyImmediate(kvp.Value);
        }
        previewTiles.Clear();

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                UpdatePreviewTile(x, y);
            }
        }
    }

    private void UpdatePreviewTile(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);
        if (previewTiles.ContainsKey(pos))
        {
            DestroyImmediate(previewTiles[pos]);
            previewTiles.Remove(pos);
        }

        TileData tileData = map[x, y];
        if (tileData != null && tileData.terrain != TileData.TerrainType.Empty)
        {

            // DefaultTile.prefab이라는 타일을 만들어둬야 함
            GameObject tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tiles/DefaultTile.prefab");

            if (tilePrefab != null)
            {
                GameObject tileInstance = Instantiate(tilePrefab, previewContainer.transform);
                //tileInstance.transform.position = new Vector3(x, 0, y);
                tileInstance.transform.position = new Vector3(x, 0, mapHeight - 1 - y);

                Tile tileComponent = tileInstance.GetComponent<Tile>();
                if (tileComponent != null)
                {
                    tileComponent.SetTileData(tileData, new Vector2Int(x, y));
                }
                previewTiles[pos] = tileInstance;
            }
        }
    }

    //private void LoadMap()
    //{
    //    string path = EditorUtility.OpenFilePanel("Select Map Prefab", MAP_PREFAB_PATH, "prefab");
    //    if (!string.IsNullOrEmpty(path))
    //    {
    //        string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
    //        loadedMapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);

    //        if (loadedMapPrefab != null)
    //        {
    //            Tile[] tiles = loadedMapPrefab.GetComponentsInChildren<Tile>();

    //            // 맵 크기 계산
    //            int maxX = int.MinValue, maxY = int.MinValue;
    //            int minX = int.MaxValue, minY = int.MaxValue;
    //            foreach (Tile tile in tiles)
    //            {
    //                maxX = Mathf.Max(maxX, tile.GridPosition.x);
    //                maxY = Mathf.Max(maxY, tile.GridPosition.y);
    //                minX = Mathf.Min(minX, tile.GridPosition.x);
    //                minY = Mathf.Min(minY, tile.GridPosition.y);
    //            }

    //            mapWidth = maxX - minX + 1;
    //            mapHeight = maxY - minY + 1;
    //            InitializeMap();

    //            foreach (Tile tile in tiles)
    //            {
    //                int x = tile.GridPosition.x;
    //                int y = tile.GridPosition.y;
    //                map[x, y] = tile; 
    //            }

    //            Repaint(); // GUI 즉시 업데이트
    //        }
    //    }
    //}
}
