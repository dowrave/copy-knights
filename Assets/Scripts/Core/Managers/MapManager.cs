using UnityEngine;
using System.Collections.Generic;

/*
 MapManager�� ����
1. �� ������ �ε� �� ����
2. Ÿ�� ���� ����
3. �� ���� ��ƿ��Ƽ �Լ� ����(���� <-> �׸��� ��ǥ)
 */
public class MapManager : MonoBehaviour
{ 
    public static MapManager Instance { get; private set; }
    
    private Map currentMap;
    public Map CurrentMap => currentMap;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitializeMap(Map map)
    {
        if (currentMap != null)
        {
            Debug.LogWarning("���� ���� �����մϴ�");
            Destroy(currentMap.gameObject);
        }

        currentMap = map;

        currentMap.Initialize(currentMap.Width, currentMap.Height, true);

        // �ٸ� �Ŵ����� �� ���� �˸�
        SpawnerManager.Instance.Initialize(currentMap);
        Debug.Log("SpawnerManager �ʱ�ȭ �Ϸ�");

        // ī�޶� ����
        InitializeCameraManager();
    }

    private void InitializeCameraManager()
    {
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.SetupForMap(currentMap);
        }
        else
        {
            Debug.LogError("ī�޶� �Ŵ��� �ν��Ͻ��� ã�� �� ����");
        }
    }

    public bool IsPositionWalkable(Vector3 worldPosition)
    {
        Vector2Int gridPos = currentMap.WorldToGridPosition(worldPosition);
        TileData tileData = currentMap.GetTileData(gridPos.x, gridPos.y);
        return tileData != null && tileData.isWalkable;
    }

    // ���� ��ǥ�� �ִ� Ÿ���� ��ȯ
    public Tile GetTileAtPosition(Vector3 worldPosition)
    {
        Vector2Int gridPosition = currentMap.WorldToGridPosition(worldPosition);
        return currentMap.GetTile(gridPosition.x, gridPosition.y);
    }


    public Tile GetTile(int worldX, int worldZ)
    {
        return currentMap.GetTile(worldX, worldZ);
    }

    public IEnumerable<Tile> GetAllTiles()
    {
        return currentMap.GetAllTiles();
    }

    /// <summary>
    /// �ش� ������Ʈ�� ���� ��ǥ�� ��ǲ���� �޾� �׸��� ��ǥ�� ��ȯ��
    /// </summary>
    public Vector2Int ConvertToGridPosition(Vector3 worldPosition)
    {
        return currentMap.WorldToGridPosition(worldPosition);
    }

    /// <summary>
    /// �׸��� ��ǥ -> ���� ��ǥ ��ȯ
    /// </summary>
    public Vector3 ConvertToWorldPosition(Vector2Int gridPosition)
    {
        return currentMap.GridToWorldPosition(gridPosition);
    }


    public Vector3 GetEndPoint()
    {
        return currentMap.FindEndPoint();
    }

    public Map GetCurrentMap()
    {
        return currentMap;
    }

    public (Vector3 position, Vector3 rotation) GetCurrentMapCameraSettings()
    {
        if (currentMap != null)
        {
            return (currentMap.CameraPosition, currentMap.CameraRotation);
        }
        return (Vector3.zero, Vector3.zero);
    }

    public int GetCurrentMapWidth()
    {
        return currentMap != null ? currentMap.Width : 0;
    }

    public int GetCurrentMapHeight()
    {
        return currentMap != null ? currentMap.Height : 0;
    }

    public bool IsTileWalkable(Vector2Int tileGridPosition)
    {
        return GetTile(tileGridPosition.x, tileGridPosition.y).IsWalkable;
    }
}