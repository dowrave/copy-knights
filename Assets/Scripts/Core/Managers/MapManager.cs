using UnityEngine;
using System.Collections.Generic;

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
            Destroy(currentMap.gameObject);
        }

        currentMap = map;

        currentMap.Initialize(currentMap.Width, currentMap.Height, true);

        // �ٸ� �Ŵ����� �� ���� �˸�
        SpawnerManager.Instance.Initialize(currentMap);

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

    // ���� ��ǥ�� �ִ� Ÿ���� ��ȯ
    public Tile GetTileAtWorldPosition(Vector3 worldPosition)
    {
        Vector2Int gridPosition = currentMap.WorldToGridPosition(worldPosition);
        return currentMap.GetTile(gridPosition.x, gridPosition.y);
    }


    public Tile GetTile(int gridX, int gridY)
    {
        return currentMap.GetTile(gridX, gridY);
    }

    public IEnumerable<Tile> GetAllTiles()
    {
        return currentMap.GetAllTiles();
    }


    // �ش� ������Ʈ�� ���� ��ǥ�� ��ǲ���� �޾� �׸��� ��ǥ�� ��ȯ��
    public Vector2Int ConvertToGridPosition(Vector3 worldPosition)
    {
        return currentMap.WorldToGridPosition(worldPosition);
    }

    // �׸��� ��ǥ -> ���� ��ǥ ��ȯ
    public Vector3 ConvertToWorldPosition(Vector2Int gridPosition)
    {
        return currentMap.GridToWorldPosition(gridPosition);
    }

    public int GetCurrentMapWidth()
    {
        return currentMap != null ? currentMap.Width : 0;
    }
}