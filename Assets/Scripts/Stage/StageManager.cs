using UnityEngine;
//using System.Collections.Generic; // IEnumerator<T> - ���׸� ����
using System.Collections; // IEnumerator - �ڷ�ƾ���� �ַ� ����ϴ� ����

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

    // ��ġ �ڽ�Ʈ
    // ���� ������ ���� ���� �󿡼��� ����
    [SerializeField] private float costIncreaseInterval = 1f; // �ڽ�Ʈ ȸ�� �ӵ��� ����
    [SerializeField] private int maxDeploymentCost = 99;
    [SerializeField] private int currentDeploymentCost = 10;
    private float currentCostGauge = 0f;

    public int CurrentDeploymentCost
    {
        get => currentDeploymentCost;
        private set
        {
            // ���ʿ��� ������Ʈ ���� - ���� ����� ���� �ڵ尡 ����ȴ�
            if (currentDeploymentCost != value)
            {
                currentDeploymentCost = value;
                OnDeploymentCostChanged?.Invoke(); // ���� ����� �� �̺�Ʈ �߻�. 
            }
        }
    }
    public float CurrentCostGauge => currentCostGauge;

    // �̺�Ʈ : System.Action�� �Ű������� ��ȯ���� ���� �޼��带 ��Ÿ���� ��������Ʈ Ÿ��.
    public event System.Action OnDeploymentCostChanged; // �̺�Ʈ �ߵ� ������ currentDeploymentCost ���� ���� ��, ���� ��ϵ� �Լ����� �����Ѵ�.

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
        StartCoroutine(IncreaseCostOverTime());
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

    private IEnumerator IncreaseCostOverTime()
    {
        while (true)
        {
            yield return null; // �� �����Ӹ��� ����
            currentCostGauge += Time.deltaTime / costIncreaseInterval;

            if (currentCostGauge >= 1f)
            {
                currentCostGauge -= 1f;
                if (currentDeploymentCost < maxDeploymentCost)
                {
                    CurrentDeploymentCost++; // ������Ƽ�� ���͵� �̷��� ����� �� �ִ� ��
                }
            }
        }
    }

    // ��ġ �� �ڽ�Ʈ ����
    public bool TryUseDeploymentCost(int cost)
    {
        if (CurrentDeploymentCost >= cost)
        {
            CurrentDeploymentCost -= cost;
            return true;
        }
        return false; 
    }

}

public enum GameState
{
    Preparation,
    Battle,
    Paused,
    GameOver
}