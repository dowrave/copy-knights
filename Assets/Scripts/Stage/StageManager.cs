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
    public GameState currentState;

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
        // ��� ��Ʈ ���� ������Ʈ ã��
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        // "Stage"�� �����ϴ� �̸��� ���� ���� ������Ʈ�� ã���ϴ�.
        foreach (GameObject obj in rootObjects)
        {
            if (obj.name.StartsWith("Stage"))
            {
                return obj;
            }
        }

        return null; // Stage�� ã�� ���� ���
    }
}

public enum GameState
{
    Preparation,
    Battle,
    Paused,
    GameOver
}