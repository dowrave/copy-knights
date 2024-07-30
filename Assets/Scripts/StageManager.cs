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
    private static StageManager instance;
    public static StageManager Instance { get; private set; }

    [SerializeField] private MapManager mapManager;
    [SerializeField] private SpawnerManager spawnerManager;
    [SerializeField] private Map currentMap;
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
        
        AssignMap(); // currentMap�� ���̾��Ű�� �� �ִ� Map �Ҵ�

        if (currentMap == null)
        {
            Debug.LogError("�������� �Ŵ����� ���� �Ҵ���� ����");
        }

        currentMap.Initialize(currentMap.Width, currentMap.Height, true);
        mapManager.InitializeMap(currentMap);
        spawnerManager.Initialize(currentMap);
        SetGameState(GameState.Preparation);
    }

    // �� �ڵ� �Ҵ� �޼���
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
            Debug.LogError("���� ã�� �� �����ϴ�.");
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