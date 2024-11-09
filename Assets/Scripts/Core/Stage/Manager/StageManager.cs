using UnityEngine;
using System.Collections; // IEnumerator - �ڷ�ƾ���� �ַ� ����ϴ� ����
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using DG.Tweening;


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

    // ���� ���� ����
    private int totalEnemyCount;
    private int killedEnemyCount;
    private int maxLifePoints = 3;
    private int currentLifePoints;
    private int passedEnemies;
    public int KilledEnemyCount
    {
        get => killedEnemyCount;
        private set
        {
            if (killedEnemyCount != value)
            {
                killedEnemyCount = value;
                OnLifePointsChanged?.Invoke(killedEnemyCount);
            }
        }
    }
    public int TotalEnemyCount => totalEnemyCount;
    public int MaxLifePoints => maxLifePoints;
    public int CurrentLifePoints
    {
        get => currentLifePoints;
        private set
        {
            if (currentLifePoints != value)
            {
                currentLifePoints = value;
                OnLifePointsChanged?.Invoke(currentLifePoints);
            }
        }
    }
    private bool isSpeedUp = false;
    private const float speedUpScale = 2f;
    private float originalTimeScale = 1f;
    private const float placementTimeScale = 0.2f;
    public bool IsSpeedUp => isSpeedUp;
    public float SpeedUpScale => speedUpScale;
    public float OriginalTimeScale => originalTimeScale;
    public float PlacementTimeScale => placementTimeScale;
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

    // ������������ �����Ǵ� deployable ��ҵ�
    [System.Serializable]
    public class StageDeployable
    {
        public GameObject deployablePrefab;
        public int maxDeployCount;
    }

    [SerializeField] private List<StageDeployable> stageDeployables = new List<StageDeployable>();

    // �̺�Ʈ
    public event System.Action OnDeploymentCostChanged; // �̺�Ʈ �ߵ� ������ currentDeploymentCost ���� ���� ��, ���� ��ϵ� �Լ����� ����
    public event System.Action<int> OnLifePointsChanged; // ������ ����Ʈ ���� �� �߻� �̺�Ʈ
    public event System.Action OnEnemyKilled; // ���� ���� ������ �߻� �̺�Ʈ

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

        DOTween.SetTweensCapacity(500, 50); // ���ÿ� ����� �ִϸ��̼��� �� / ���� �ִϸ��̼��� ���������� ����Ǵ� ��
    }

    private void Start()
    {
        // Awake���� UIManager null ������ ���ڱ� ���� Start�� �Űܳ���
        UIManager.Instance.UpdateSpeedUpButtonVisual();
        UIManager.Instance.UpdatePauseButtonVisual();

        if (GameManagement.Instance == null && 
            GameManagement.Instance.StageLoader != null)
        {
            // StageLoader�� ���� �ʱ�ȭ ���
            // �� ��ȯ�� ���� ����� ���, Start���� �����ϸ� ����������Ŭ�� ����ġ�ϹǷ� 
            // StageLoader���� �������� ������ ó���Ѵ�.
            return;
        }

        // ���� ������ ��� 
        PrepareStage();
        StartStage();
    }

    public void PrepareStage()
    {
        Debug.Log("�������� �غ�");
        SetGameState(GameState.Preparation);
        StartCoroutine(IncreaseCostOverTime());

        // ���� �ʱ�ȭ
        totalEnemyCount = CalculateTotalEnemyCount();
        killedEnemyCount = 0;
        CurrentLifePoints = maxLifePoints;

        UIManager.Instance.InitializeUI();

    }

    public void StartStage()
    {
        Debug.Log("�������� ����");
        SetGameState(GameState.Battle);
        SpawnerManager.Instance.StartSpawning();
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

    public List<StageDeployable> GetStageDeployables()
    {
        return stageDeployables;
    }

    public void SetGameState(GameState gameState)
    {
        currentState = gameState;

        switch (gameState)
        {
            case GameState.Battle:
                Time.timeScale = isSpeedUp ? SpeedUpScale : OriginalTimeScale;
                UIManager.Instance.HidePauseOverlay();
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                UIManager.Instance.ShowPauseOverlay();
                break;
            case GameState.GameOver:
            case GameState.GameWin:
                Time.timeScale = 0f;
                UIManager.Instance.HidePauseOverlay();
                break;
        }

        UIManager.Instance.UpdatePauseButtonVisual();
        // �ٸ� �ʿ��� ���� ���� ����...
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
        UIManager.Instance.UpdateEnemyKillCountText();

        // ��� "������" ���� �����ϸ� ������ ���� �� �ٸ��� ��� ��
        if (killedEnemyCount + passedEnemies >= totalEnemyCount)
        {
            GameWin();
        }
    }

    public void OnEnemyReachDestination()
    {
        if (currentState == GameState.GameOver || currentState == GameState.GameWin) return; 

        
        currentLifePoints--; // OnLifePointsChanged : ������� ���̸� �ڵ����� UI ������Ʈ �߻�
        passedEnemies++;
        //UpdateUI();

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

    public void SlowDownTime()
    {
        Time.timeScale = PlacementTimeScale;
    }

    public void UpdateTimeScale()
    {
        if (currentState != GameState.Paused)
        {
            Time.timeScale = isSpeedUp ? SpeedUpScale : OriginalTimeScale;
        }
    }

    public void ToggleSpeedUp()
    {
        if (currentState != GameState.Battle) return;

        isSpeedUp = !isSpeedUp;
        UpdateTimeScale();
        UIManager.Instance.UpdateSpeedUpButtonVisual();
    }

    public void TogglePause()
    {
        Debug.Log("Pause ����");

        if (currentState == GameState.GameOver || currentState == GameState.GameWin) return;

        if (currentState == GameState.Paused)
        {
            SetGameState(GameState.Battle);
        }
        else if (currentState == GameState.Battle)
        {
            SetGameState(GameState.Paused);
        }
        UIManager.Instance.UpdatePauseButtonVisual();
    }

    public void RecoverDeploymentCost(int amount)
    {
        CurrentDeploymentCost = Mathf.Min(CurrentDeploymentCost + amount, maxDeploymentCost);
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