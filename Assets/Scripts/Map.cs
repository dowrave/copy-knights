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


    public void Initialize(int width, int height, bool load = false)
    {
        this.width = width;
        this.height = height;

        // ���� �ҷ����� ��Ȳ�� ���� �����ϴ� ��Ȳ�� �����Ѵ�.
        if (load)
        {
            LoadExistingMap();
        }
        else
        {
            CreateNewMap();
        }
    }

    private void CreateNewMap()
    {
        tiles = new TileData[width, height];
    }

    // �������� �ڽ� ������Ʈ��κ��� Ÿ�� �����͵��� �ε��Ѵ�
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
                //Vector2Int gridPos = tile.GridPosition;

                // Ÿ���� ���� ��ġ�� ������� �׸��� ��ġ ���
                int x = Mathf.RoundToInt(child.localPosition.x);
                int y = Mathf.RoundToInt(height - 1 - child.localPosition.z); // Z ��ǥ�� Y �׸��� ��ġ�� ��ȯ

                if (IsValidPosition(x, y))
                {
                    tile.SetTileData(tile.data, new Vector2Int(x, y));
                    tiles[x, y] = tile.data;
                    Debug.Log($"Loaded tile at ({x}, {y}): {tile.data.TileName}");
                }
            }
        }
    }


    public void SetTile(int x, int y, TileData tileData)
    {
        if (IsValidPosition(x, y))
        {
            tiles[x, y] = tileData;
        }
    }

    public TileData GetTile(int x, int y)
    {
        if (tiles == null)
        {
            Debug.LogError("Ÿ�� �迭�� �ʱ�ȭ���� ����");
            return null;
        }
        return IsValidPosition(x, y) ? tiles[x, y] : null; 
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height; 
    }

    public List<Vector2Int> GetTilesOfType(TileData.TerrainType terrainType)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        for (int x=0; x < width; x++)
        {
            for (int y=0; y < height; y++)
            {
                if (tiles[x, y] != null && tiles[x, y].terrain == terrainType)
                {
                    result.Add(new Vector2Int(x, y));
                }
            }
        }

        return result;
    }

    public void Resize(int newWidth, int newHeight, TileData[,] newTiles)
    {
        width = newWidth;
        height = newHeight;
        tiles = newTiles;

        // ���� Ÿ�� ���� ������Ʈ ����
        //foreach (Transform child in transform)
        //{
        //    DestroyImmediate(child.gameObject);
        //}

        // ���ο� Ÿ�Ͽ� ���� ���� ������Ʈ ����
        //for (int x = 0; x < width; x++)
        //{
        //    for (int y=0; y < height; y++)
        //    {
        //        SetTile(x, y, tiles[x, y]);
        //    }
        //}
    }
}
