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

        // 맵을 불러오는 상황과 새로 생성하는 상황을 구분한다.
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
                //Vector2Int gridPos = tile.GridPosition;

                // 타일의 로컬 위치를 기반으로 그리드 위치 계산
                int x = Mathf.RoundToInt(child.localPosition.x);
                int y = Mathf.RoundToInt(height - 1 - child.localPosition.z); // Z 좌표를 Y 그리드 위치로 변환

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
            Debug.LogError("타일 배열이 초기화되지 않음");
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

        // 기존 타일 게임 오브젝트 제거
        //foreach (Transform child in transform)
        //{
        //    DestroyImmediate(child.gameObject);
        //}

        // 새로운 타일에 대한 게임 오브젝트 생성
        //for (int x = 0; x < width; x++)
        //{
        //    for (int y=0; y < height; y++)
        //    {
        //        SetTile(x, y, tiles[x, y]);
        //    }
        //}
    }
}
