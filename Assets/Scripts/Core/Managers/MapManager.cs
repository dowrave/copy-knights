using UnityEngine;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    public static MapManager? Instance { get; private set; }

    private Map? currentMap;
    public Map? CurrentMap => currentMap;

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
            currentMap = null;
        }

        currentMap = map;
        currentMap.InitializeOnStage(currentMap.Width, currentMap.Height);

        // �ٸ� �Ŵ����� �� ���� �˸�
        SpawnerManager.Instance!.Initialize(currentMap);
        

        // ī�޶� ����
        InitializeCameraManager();
    }

    private void InitializeCameraManager()
    {
        CameraManager.Instance!.SetupForMap(currentMap);
    }

    // ���� ��ǥ�� �ִ� Ÿ���� ��ȯ
    public Tile? GetTileAtWorldPosition(Vector3 worldPosition)
    {
        if (currentMap == null)
        {
            Debug.LogError("���� ���� �ʱ�ȭ���� �ʾҽ��ϴ�.");
            return null;
        }

        Vector2Int gridPosition = currentMap.WorldToGridPosition(worldPosition);
        return currentMap.GetTile(gridPosition.x, gridPosition.y);
    }

    public Tile? GetTile(int gridX, int gridY)
    {
        if (currentMap == null)
        {
            Debug.LogError("���� ���� �ʱ�ȭ���� �ʾҽ��ϴ�.");
            return null;
        }

        return currentMap.GetTile(gridX, gridY);
        
    }

    public IEnumerable<Tile> GetAllTiles()
    {
        if (currentMap == null)
        {
            Debug.LogError("���� ���� �ʱ�ȭ���� �ʾҽ��ϴ�.");
            return new List<Tile>();
        }
        return currentMap.GetAllTiles();
    }

    // �ش� ������Ʈ�� ���� ��ǥ�� ��ǲ���� �޾� �׸��� ��ǥ�� ��ȯ��
    public Vector2Int ConvertToGridPosition(Vector3 worldPosition)
    {
        if (currentMap == null)
        {
            Debug.LogError("���� ���� �ʱ�ȭ���� �ʾҽ��ϴ�.");
            return Vector2Int.zero;
        }

        return currentMap.WorldToGridPosition(worldPosition);
    }

    // �׸��� ��ǥ -> ���� ��ǥ ��ȯ
    public Vector3 ConvertToWorldPosition(Vector2Int gridPosition)
    {
        if (currentMap == null)
        {
            Debug.LogError("���� ���� �ʱ�ȭ���� �ʾҽ��ϴ�.");
            return Vector3.zero;
        }
        return currentMap.GridToWorldPosition(gridPosition);
    }

    public int GetCurrentMapWidth()
    {
        return currentMap != null ? currentMap.Width : 0;
    }
}
