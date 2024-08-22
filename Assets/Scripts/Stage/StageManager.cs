using UnityEngine;
//using System.Collections.Generic; // IEnumerator<T> - ���׸� ����
using System.Collections; // IEnumerator - �ڷ�ƾ���� �ַ� ����ϴ� ����
using TMPro;
using UnityEngine.SceneManagement;

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

    // ��� UI ���
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI lifePointsText;

    // ���� ���� ����
    private int totalEnemyCount;
    private int killedEnemyCount;
    private int maxLifePoints = 3;
    private int currentLifePoints;
    private int passedEnemies;

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

        // ���� �ʱ�ȭ
        totalEnemyCount = CalculateTotalEnemyCount();
        killedEnemyCount = 0;
        currentLifePoints = maxLifePoints;

        UpdateUI();
    }

    private int CalculateTotalEnemyCount()
    {
        int count = 0;
        EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>();
        foreach (var spawner in spawners)
        {
            count += spawner.enemySpawnList.Count;
        }
        return count;
    }

    private void UpdateUI()
    {
        if (enemyCountText != null)
        {
            enemyCountText.text = $"{killedEnemyCount} / {totalEnemyCount}";

        }

        if (lifePointsText != null)
        {
            lifePointsText.text = $"{currentLifePoints}";
        }
    } 

    public void StartBattle()
    {
        SetGameState(GameState.Battle);
        SpawnerManager.Instance.StartSpawning();
    }

    private void SetGameState(GameState gameState)
    {
        currentState = gameState; 

        switch (gameState)
        {
            case GameState.Battle:
                Time.timeScale = 1f;
                break;

            // switch�� �Ѳ����� ó���ϱ�
            case GameState.Paused:
            case GameState.GameOver:
            case GameState.GameWin:
                Time.timeScale = 0f;
                break;
        }
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
        if (currentState == GameState.GameOver) return false;

        if (CurrentDeploymentCost >= cost)
        {
            CurrentDeploymentCost -= cost;
            return true;
        }
        return false; 
    }

    // �� ��� �� ȣ��
    public void OnEnemyDefeated()
    {
        if (currentState == GameState.GameOver) return;

        killedEnemyCount++;
        UpdateUI();

        // ��� "������" ���� �����ϸ� ������ ���� �� �ٸ��� ��� ��
        if (killedEnemyCount + passedEnemies >= totalEnemyCount)
        {
            GameWin();
        }
    }

    public void OnEnemyReachDestination()
    {
        if (currentState == GameState.GameOver || currentState == GameState.GameWin) return; 

        currentLifePoints--;
        passedEnemies++;
        UpdateUI();

        if (currentLifePoints <= 0)
        {
            GameOver();
        }
        else if (killedEnemyCount + passedEnemies >= totalEnemyCount)
        {
            GameWin();
        }
    }

    private void GameWin()
    { 
        SetGameState(GameState.GameWin);
        Time.timeScale = 0;
        UIManager.Instance.ShowGameWinUI();
        StopAllCoroutines();
    }

    private void GameOver()
    { 
        SetGameState(GameState.GameOver);
        Time.timeScale = 0; // ���� �Ͻ� ����
        UIManager.Instance.ShowGameOverUI();
        StopAllCoroutines();
    }

    private void ReturnToMenu()
    {
        Time.timeScale = 1; // ���� �ӵ� ����
        SceneManager.LoadScene("MainMenu");
    }
}

public enum GameState
{
    Preparation,
    Battle,
    Paused,
    GameWin,
    GameOver
}