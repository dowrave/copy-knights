using UnityEngine;
using System;
using UnityEditor.Build.Content;
using System.Collections.Generic;

[Serializable]
public class MapData
{
    public int width;
    public int height;
    //public TileType[] tiles;
}

public class MapManager : MonoBehaviour
{
    [SerializeField] private GameObject mapPrefab;
    [SerializeField] private EnemySpawner enemySpawner;

    // ī�޶� ����
    [SerializeField] private float cameraHeight = 8f;
    [SerializeField] private float cameraAngle = 75f;
    [SerializeField] private float cameraOffsetZ = 2f;

    private Vector3 startPoint;
    private Vector3 endPoint;
    private Tile[,] tiles;
    private int mapWidth, mapHeight;

    private void Start()
    {
        LoadMapFromPrefab();
        FindStartAndEndPoints();
        SetEnemySpawnerPosition();
        AdjustCameraPosition(); 
    }

    private void LoadMapFromPrefab()
    {
        if (mapPrefab != null)
        {
            GameObject mapInstance = Instantiate(mapPrefab, transform);
            Tile[] allTiles = mapInstance.GetComponentsInChildren<Tile>();

            // �� ũ�� ����
            int maxX = 0, maxY = 0;
            foreach (Tile tile in allTiles)
            {
                Vector2Int gridPos = tile.GridPosition;
                maxX = Mathf.Max(maxX, gridPos.x);
                maxY = Mathf.Max(maxY, gridPos.y);
            }

            tiles = new Tile[maxX + 1, maxY + 1];

            foreach (Tile tile in allTiles)
            {
                Vector2Int gridPos = tile.GridPosition;
                tiles[gridPos.x, gridPos.y] = tile;
            }
        }
    }

    private void FindStartAndEndPoints()
    {
        for (int x = 0; x < tiles.GetLength(0);x++)
        {
            for (int y=0; y < tiles.GetLength(1);y++)
            {
                if (tiles[x, y] != null) 
                { 
                    if (tiles[x, y].data.isStartPoint)
                    {
                        startPoint = tiles[x, y].transform.position + Vector3.up * 0.5f;
                    }
                    else if (tiles[x, y].data.isEndPoint)
                    {
                        endPoint = tiles[x, y].transform.position + Vector3.up * 0.5f;
                    }
                } 
            }
        }
    }

    public Vector3 GetStartPoint() => startPoint;
    public Vector3 GetEndPoint() => endPoint;

    public bool IsTileWalkable(int x, int y)
    {
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight) return false;
        return tiles[x, y] != null && tiles[x, y].data.isWalkable;
    }

    public Vector3 GetTilePosition(int x, int y)
    {
        if (tiles[x, y] != null) 
        { 
            return tiles[x, y].transform.position + Vector3.up * 0.5f;
        }
        return Vector3.zero; 
    }

    public Tile GetTile(int x, int y)
    {
        if (x >= 0 && x < tiles.GetLength(0) && y >= 0 && y < tiles.GetLength(1))
        {
            return tiles[x, y];
        }
        return null;
    }

    public IEnumerable<Tile> GetAllTiles()
    {
        for (int x = 0; x < tiles.GetLength(0); x++)
        {
            for (int y = 0; y < tiles.GetLength(1); y++)
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
            Vector3 mapCenter = new Vector3(mapWidth / 2f - 0.5f, 0f, mapHeight / 2f - 0.5f);

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
            float mapSize = Mathf.Max(mapWidth, mapHeight);
            mainCamera.fieldOfView = 2f * Mathf.Atan(mapSize / (2f * cameraHeight)) * Mathf.Rad2Deg;
        }
    }
}