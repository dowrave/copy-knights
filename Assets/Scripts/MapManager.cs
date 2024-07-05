using UnityEngine;
using System;
using UnityEditor.Build.Content;
using System.Collections.Generic;

[Serializable]
public class MapData
{
    public int width;
    public int height;
    public TileType[] tiles;
}

public class MapManager : MonoBehaviour
{
    private float groundHeight = 0.1f; // 바닥 타일의 높이
    private float hillHeight = 0.5f; // 언덕 타일의 높이
    private float tileScale = 0.98f;

    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private TextAsset mapDataJson;
    [SerializeField] private Material planeMaterial; // 바닥 Plane의 Material
    [SerializeField] private Color planeColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private EnemySpawner enemySpawner;

    // 카메라 위치 / 회전 필드
    [SerializeField] private float cameraHeight = 8f;
    [SerializeField] private float cameraAngle = 75f;
    [SerializeField] private float cameraOffsetZ = 2f; // 값이 커지면 아래로, 작으면 위로 이동한다.


    private Vector3 startPoint;
    private Vector3 endPoint;
    private Tile[,] tiles;
    private MapData mapData;

    private void Start()
    {
        LoadMapData();
        CreateGroundPlane();
        GenerateMap();
        FindStartAndEndPoints();
        SetEnemySpawnerPosition();
        AdjustCameraPosition(); 
    }

    private void LoadMapData()
    {
        mapData = JsonUtility.FromJson<MapData>(mapDataJson.text);
    }

    private void CreateGroundPlane()
    {
        GameObject planeObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
        planeObject.transform.SetParent(transform);
        planeObject.transform.localPosition = new Vector3(mapData.width / 2f - 0.5f, 0, mapData.height / 2f - 0.5f);
        planeObject.transform.localScale = new Vector3(mapData.width / 10f, 1, mapData.height / 10f);

        Renderer planeRenderer = planeObject.GetComponent<Renderer>();
        if (planeMaterial != null)
        {
            Material newMaterial = new Material(planeMaterial);
            newMaterial.color = planeColor;
            planeRenderer.material = newMaterial;
        }
        else
        {
            planeRenderer.material.color = planeColor;
        }
    }

    /// <summary>
    /// 맵은 2차원으로 표현됨 - xy 평면이라는 말을 쓰되, 여기서의 y는 유니티 엔진 상에서 z축임! 
    /// </summary>
    private void GenerateMap()
    {
        tiles = new Tile[mapData.width, mapData.height];

        for (int x = 0; x < mapData.width; x++)
        {
            for (int y = 0; y < mapData.height; y++)
            {
                //int index = y * mapData.width + x; // 이대로 맵을 구현하면 y축으로 반전돼서 구현됨
                int index = (mapData.height - 1 - y) * mapData.width + x; // 맵을 보이는 그대로 구현시키기

                TileType tileType = mapData.tiles[index];
                if (tileType != TileType.Empty) { 
                    Vector3 position = new Vector3(x, 0.5f, y); // Plane 위에 배치되도록 y = 0.5로 설정
                    GameObject tileObject = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                    tileObject.transform.localScale = new Vector3(tileScale, 1f, tileScale);
                    Tile tile = tileObject.GetComponent<Tile>();

                    float tileHeight = (tileType == TileType.Hill) ? hillHeight : groundHeight;
                    tile.Initialize(tileType, new Vector2Int(x, y), tileHeight);

                    tiles[x, y] = tile;
                }
            }
        }
    }

    private void FindStartAndEndPoints()
    {
        for (int x = 0; x <mapData.width;x++)
        {
            for (int y=0; y < mapData.height;y++)
            {
                if (tiles[x, y] != null) 
                { 
                    if (tiles[x, y].Type == TileType.Start)
                    {
                        startPoint = new Vector3(x, tiles[x, y].transform.position.y + 0.5f, y);
                    }
                    else if (tiles[x, y].Type == TileType.End)
                    {
                        endPoint = new Vector3(x, tiles[x, y].transform.position.y + 0.5f, y);
                    }
                } 
            }
        }

        //if (startPoint != Vector3.zero && endPoint != Vector3.zero)
        //{
        //    enemySpawner.SetPathPoints(startPoint, endPoint);
        //}
        //Debug.LogError("Start Tile Not Found !");
    }

    public Vector3 GetStartPoint() => startPoint;
    public Vector3 GetEndPoint() => endPoint; 

    public bool IsTileWalkable(int x, int y)
    {
        if (x < 0 || x >= mapData.width || y < 0 || y >= mapData.height) return false;
        return tiles[x, y] != null && tiles[x, y].IsWalkable;
    }

    public Vector3 GetTilePosition(int x, int y)
    {
        return tiles[x, y].transform.position + Vector3.up * 0.5f;
    }

    public Tile GetTile(int x, int y)
    {
        if (x < 0 || x >= mapData.width || y < 0 || y >= mapData.height) return null;
        return tiles[x, y];
    }

    public IEnumerable<Tile> GetAllTiles()
    {
        for (int x = 0; x < mapData.width; x++)
        {
            for (int y = 0; y < mapData.height; y++)
            {
                if (tiles[x, y] != null)
                {
                    yield return tiles[x, y];
                }
            }
        }
    }

    private void SetEnemySpawnerPosition()
    {
        if (enemySpawner != null && startPoint != Vector3.zero && endPoint != Vector3.zero) 
        {
            enemySpawner.transform.position = startPoint;
            enemySpawner.SetPathPoints(startPoint, endPoint);
        }
    }

    // 맵의 크기에 따른 카메라 위치 자동 조정
    private void AdjustCameraPosition()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // 맵의 중심
            Vector3 mapCenter = new Vector3(mapData.width / 2f - 0.5f, 0f, mapData.height / 2f - 0.5f);

            mainCamera.transform.rotation = Quaternion.Euler(cameraAngle, 0f, 0f);

            // 카메라 위치 계산
            float zOffset = cameraOffsetZ * Mathf.Tan((90f - cameraAngle) * Mathf.Deg2Rad);

            Vector3 cameraPosition = new Vector3(
                mapCenter.x,
                cameraHeight,
                mapCenter.z - zOffset
            );
            mainCamera.transform.position = cameraPosition;

            // 카메라가 맵 중심을 보도록 설정
            mainCamera.transform.LookAt(mapCenter);

            // 시야각 조정
            float mapSize = Mathf.Max(mapData.width, mapData.height);
            mainCamera.fieldOfView = 2f * Mathf.Atan(mapSize / (2f * cameraHeight)) * Mathf.Rad2Deg;
        }
    }
}