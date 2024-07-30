using UnityEngine;
using System.Collections.Generic;

/*
 StageManager의 역할
1. 전체 게임 흐름 제어 
2. 게임 상태 관리
3. 적 스포너 관리
4. 플레이어 자원 관리
5. UI 업데이트 트리거
6. 승리 / 패배 조건 체크
7. 아군 캐릭터 배치 로직 관리
 */
public class StageManager : MonoBehaviour
{
    private static StageManager instance;
    public static StageManager Instance { get; private set; }

    [SerializeField] private MapManager mapManager;
    [SerializeField] private SpawnerManager spawnerManager;
    [SerializeField] private Map currentMap;
    private GameState currentState;

    private void Awake()
    {
        // 싱글톤 보장
       if (Instance == null)
        {
            Instance = this; 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Debug.Log("스테이지 준비");
        InitializeStage(); // 스테이지 준비

        Debug.Log("스테이지 시작");
        StartBattle(); // 게임 시작
    }

    private void InitializeStage()
    {
        
        AssignMap(); // currentMap에 하이어라키에 떠 있는 Map 할당

        if (currentMap == null)
        {
            Debug.LogError("스테이지 매니저에 맵이 할당되지 않음");
        }

        currentMap.Initialize(currentMap.Width, currentMap.Height, true);
        mapManager.InitializeMap(currentMap);
        spawnerManager.Initialize(currentMap);
        SetGameState(GameState.Preparation);
    }

    // 맵 자동 할당 메서드
    private void AssignMap()
    {
        currentMap = FindObjectOfType<Map>();

        if (currentMap == null)
        {
            GameObject stageObject = FindStageObject();
            if (stageObject != null)
            {
                currentMap = stageObject.GetComponentInChildren<Map>();
            }
        }

        if (currentMap == null)
        {
            Debug.LogError("맵을 찾을 수 없습니다.");
        }
    }

    public void StartBattle()
    {
        SetGameState(GameState.Battle);
        spawnerManager.StartSpawning();
    }

    private void SetGameState(GameState gameState)
    {
        currentState = gameState; 
    }

    public static GameObject FindStageObject()
    {
        // 모든 루트 게임 오브젝트 찾기
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        // "Stage"로 시작하는 이름을 가진 게임 오브젝트를 찾습니다.
        foreach (GameObject obj in rootObjects)
        {
            if (obj.name.StartsWith("Stage"))
            {
                return obj;
            }
        }

        return null; // Stage를 찾지 못한 경우
    }
}

public enum GameState
{
    Preparation,
    Battle,
    Paused,
    GameOver
}