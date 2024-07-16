using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class Map : MonoBehaviour
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private TileData[,] tiles;

    public int Width => width;
    public int Height => height;

    [SerializeField] private GameObject enemySpawnerPrefab;
    private MapManager mapManager; 


    public void Initialize(int width, int height, bool load = false, MapManager manager = null)
    {
        this.width = width;
        this.height = height;
        this.mapManager = manager;

        // 맵을 불러오는 상황과 새로 생성하는 상황을 구분한다.
        if (load)
        {
            LoadExistingMap();
        }
        else
        {
            CreateNewMap();
        }
        AssignSpawnersToStartTiles();
    }

    private void CreateNewMap()
    {
        tiles = new TileData[width, height];
    }

    // 프리팹의 자식 오브젝트들로부터 타일 데이터들을 로드한다
    public void LoadExistingMap()
    {
        if (tiles == null || tiles.GetLength(0) != width || tiles.GetLength(1) != height)
        {
            tiles = new TileData[width, height];
        }

        LoadTilesFromChildren();
    }

    private void LoadTilesFromChildren()
    {
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent<Tile>(out Tile tile))
            {
                // 타일의 로컬 위치를 기반으로 그리드 위치 계산
                int x = Mathf.RoundToInt(child.localPosition.x);
                int y = Mathf.RoundToInt(height - 1 - child.localPosition.z); // Z 좌표를 Y 그리드 위치로 변환

                if (IsValidPosition(x, y))
                {
                    tile.SetTileData(tile.data, new Vector2Int(x, y));
                    tiles[x, y] = tile.data;
                    Debug.Log($"Loaded tile at ({x}, {y}): {tile.data.TileName}, IsEndPoint: {tile.data.isEndPoint}");

                }
            }
        }
        Debug.Log($"Finished loading tiles. Array dimensions: {tiles.GetLength(0)}x{tiles.GetLength(1)}");
    }
    public void SetEnemySpawnerPrefab(GameObject prefab)
    {
        enemySpawnerPrefab = prefab;
    }

    public void SetTile(int x, int y, TileData tileData)
    {
        if (IsValidPosition(x, y))
        {
            tiles[x, y] = tileData;
            //UpdateTileVisuals(x, y);

            AssignOrRemoveSpawner(x, y, tileData != null && tileData.isStartPoint);
        }
    }

    private void AssignSpawnersToStartTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y] != null && tiles[x, y].isStartPoint)
                {
                    AssignOrRemoveSpawner(x, y, true);
                }
            }
        }
    }
    private void AssignOrRemoveSpawner(int x, int y, bool assign)
    {
        GameObject tileObject = transform.Find($"Tile_{x}_{y}")?.gameObject;
        if (tileObject != null)
        {
            Transform spawnerTransform = tileObject.transform.Find("EnemySpawner");
            //Renderer renderer = tileObject.GetComponentInChildren<Renderer>();
            if (assign)
            {
                if (spawnerTransform == null)
                {
                    // 자식 오브젝트로 스포너 추가
                    GameObject spawnerObject; 
                    if (enemySpawnerPrefab != null)
                    {
                        spawnerObject = Instantiate(enemySpawnerPrefab, tileObject.transform);
                    }
                    else
                    {
                        spawnerObject = new GameObject("EnemySpawner");
                        spawnerObject.transform.SetParent(tileObject.transform);
                        spawnerObject.AddComponent<EnemySpawner>();
                    }
                    spawnerObject.transform.localPosition = Vector3.zero;
                }
            }
            else
            {
                if (spawnerTransform != null)
                {
                    DestroyImmediate(spawnerTransform.gameObject);
                }
                // 타일 색상 원래대로 복구
                //tileObject.GetComponent<Renderer>()?.material.SetColor("_Color", Color.white);
            }
        }
    }

    public TileData GetTile(int x, int y)
    {
        if (tiles == null)
        {
            Debug.LogError("타일 배열이 초기화되지 않음");
            return null;
        }
        return IsValidPosition(x, y) ? tiles[x, y] : null; 
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height; 
    }

    public IEnumerable<Tile> GetAllTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject tileObject = transform.Find($"Tile_{x}_{y}")?.gameObject;
                if (tileObject != null)
                {
                    Tile tile = tileObject.GetComponent<Tile>();
                    if (tile != null)
                    {
                        yield return tile;
                    }
                }
            }
        }
    }

    public void Resize(int newWidth, int newHeight, TileData[,] newTiles)
    {
        width = newWidth;
        height = newHeight;
        tiles = newTiles;
    }

    public Vector3 FindEndPoint()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileData tileData = GetTile(x, y);
                Debug.Log(tileData);
                if (tileData != null && tileData.isEndPoint)
                {
                    return GetTilePosition(x, y);
                }
            }
        }
        Debug.LogWarning("도착점이 맵에 존재하지 않음!");
        return Vector3.zero;
    }

    private Vector3 GetTilePosition(int x, int y)
    {
        return new Vector3(x, 0, y) + Vector3.up * 0.5f; // 타일 중앙의 상단에 위치함
    }
}
