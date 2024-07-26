using UnityEngine;
using System.Collections.Generic;

/*
 StageManager�� ����
1. ��ü ���� �帧 ���� 
2. ���� ���� ����
3. �� ������ ����
4. �÷��̾� �ڿ� ����
5. UI ������Ʈ Ʈ����
6. �¸� / �й� ���� üũ
7. �Ʊ� ĳ���� ��ġ ���� ����
 */
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    [SerializeField] private MapManager mapManager;
    [SerializeField] private SpawnerManager spawnerManager;
    private GameState currentState;

    private void Awake()
    {
        // �̱��� ����
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
        Debug.Log("�������� �غ�");
        InitializeStage(); // �������� �غ�

        Debug.Log("�������� ����");
        StartBattle(); // ���� ����
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