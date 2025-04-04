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

        // 다른 매니저에 맵 설정 알림
        SpawnerManager.Instance!.Initialize(currentMap);
        

        // 카메라 설정
        InitializeCameraManager();
    }

    private void InitializeCameraManager()
    {
        CameraManager.Instance!.SetupForMap(currentMap);
    }

    // 월드 좌표에 있는 타일을 반환
    public Tile? GetTileAtWorldPosition(Vector3 worldPosition)
    {
        if (currentMap == null)
        {
            Debug.LogError("현재 맵이 초기화되지 않았습니다.");
            return null;
        }

        Vector2Int gridPosition = currentMap.WorldToGridPosition(worldPosition);
        return currentMap.GetTile(gridPosition.x, gridPosition.y);
    }

    public Tile? GetTile(int gridX, int gridY)
    {
        if (currentMap == null)
        {
            Debug.LogError("현재 맵이 초기화되지 않았습니다.");
            return null;
        }

        return currentMap.GetTile(gridX, gridY);
        
    }

    public IEnumerable<Tile> GetAllTiles()
    {
        if (currentMap == null)
        {
            Debug.LogError("현재 맵이 초기화되지 않았습니다.");
            return new List<Tile>();
        }
        return currentMap.GetAllTiles();
    }

    // 해당 오브젝트의 월드 좌표를 인풋으로 받아 그리드 좌표를 반환함
    public Vector2Int ConvertToGridPosition(Vector3 worldPosition)
    {
        if (currentMap == null)
        {
            Debug.LogError("현재 맵이 초기화되지 않았습니다.");
            return Vector2Int.zero;
        }

        return currentMap.WorldToGridPosition(worldPosition);
    }

    // 그리드 좌표 -> 월드 좌표 변환
    public Vector3 ConvertToWorldPosition(Vector2Int gridPosition)
    {
        if (currentMap == null)
        {
            Debug.LogError("현재 맵이 초기화되지 않았습니다.");
            return Vector3.zero;
        }
        return currentMap.GridToWorldPosition(gridPosition);
    }

    public int GetCurrentMapWidth()
    {
        return currentMap != null ? currentMap.Width : 0;
    }
}
