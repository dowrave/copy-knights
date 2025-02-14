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

        // 다른 매니저에 맵 설정 알림
        SpawnerManager.Instance.Initialize(currentMap);

        // 카메라 설정
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
            Debug.LogError("카메라 매니저 인스턴스를 찾을 수 없음");
        }
    }

    // 월드 좌표에 있는 타일을 반환
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


    // 해당 오브젝트의 월드 좌표를 인풋으로 받아 그리드 좌표를 반환함
    public Vector2Int ConvertToGridPosition(Vector3 worldPosition)
    {
        return currentMap.WorldToGridPosition(worldPosition);
    }

    // 그리드 좌표 -> 월드 좌표 변환
    public Vector3 ConvertToWorldPosition(Vector2Int gridPosition)
    {
        return currentMap.GridToWorldPosition(gridPosition);
    }

    public int GetCurrentMapWidth()
    {
        return currentMap != null ? currentMap.Width : 0;
    }
}