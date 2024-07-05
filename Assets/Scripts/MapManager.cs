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
    private float groundHeight = 0.1f; // �ٴ� Ÿ���� ����
    private float hillHeight = 0.5f; // ��� Ÿ���� ����
    private float tileScale = 0.98f;

    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private TextAsset mapDataJson;
    [SerializeField] private Material planeMaterial; // �ٴ� Plane�� Material
    [SerializeField] private Color planeColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private EnemySpawner enemySpawner;

    // ī�޶� ��ġ / ȸ�� �ʵ�
    [SerializeField] private float cameraHeight = 8f;
    [SerializeField] private float cameraAngle = 75f;
    [SerializeField] private float cameraOffsetZ = 2f; // ���� Ŀ���� �Ʒ���, ������ ���� �̵��Ѵ�.


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
    /// ���� 2�������� ǥ���� - xy ����̶�� ���� ����, ���⼭�� y�� ����Ƽ ���� �󿡼� z����! 
    /// </summary>
    private void GenerateMap()
    {
        tiles = new Tile[mapData.width, mapData.height];

        for (int x = 0; x < mapData.width; x++)
        {
            for (int y = 0; y < mapData.height; y++)
            {
                //int index = y * mapData.width + x; // �̴�� ���� �����ϸ� y������ �����ż� ������
                int index = (mapData.height - 1 - y) * mapData.width + x; // ���� ���̴� �״�� ������Ű��

                TileType tileType = mapData.tiles[index];
                if (tileType != TileType.Empty) { 
                    Vector3 position = new Vector3(x, 0.5f, y); // Plane ���� ��ġ�ǵ��� y = 0.5�� ����
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

    // ���� ũ�⿡ ���� ī�޶� ��ġ �ڵ� ����
    private void AdjustCameraPosition()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // ���� �߽�
            Vector3 mapCenter = new Vector3(mapData.width / 2f - 0.5f, 0f, mapData.height / 2f - 0.5f);

            mainCamera.transform.rotation = Quaternion.Euler(cameraAngle, 0f, 0f);

            // ī�޶� ��ġ ���
            float zOffset = cameraOffsetZ * Mathf.Tan((90f - cameraAngle) * Mathf.Deg2Rad);

            Vector3 cameraPosition = new Vector3(
                mapCenter.x,
                cameraHeight,
                mapCenter.z - zOffset
            );
            mainCamera.transform.position = cameraPosition;

            // ī�޶� �� �߽��� ������ ����
            mainCamera.transform.LookAt(mapCenter);

            // �þ߰� ����
            float mapSize = Mathf.Max(mapData.width, mapData.height);
            mainCamera.fieldOfView = 2f * Mathf.Atan(mapSize / (2f * cameraHeight)) * Mathf.Rad2Deg;
        }
    }
}