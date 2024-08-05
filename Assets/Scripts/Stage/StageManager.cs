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
    public static StageManager Instance { get; private set; }
    public GameState currentState;

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
        MapManager.Instance.InitializeMap();
        SetGameState(GameState.Preparation);
    }

    public void StartBattle()
    {
        SetGameState(GameState.Battle);
        SpawnerManager.Instance.StartSpawning();
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