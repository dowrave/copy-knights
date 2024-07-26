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
    [SerializeField] private MapManager mapManager;
    [SerializeField] private SpawnerManager spawnerManager;
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
        mapManager.InitializeMap();
        spawnerManager.Initialize(mapManager);
        SetGameState(GameState.Preparation);
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
}

public enum GameState
{
    Preparation,
    Battle,
    Paused,
    GameOver
}