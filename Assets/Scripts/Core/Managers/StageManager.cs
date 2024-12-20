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
    private StageData stageData;
    public StageData StageData => stageData;

    // ��ġ �ڽ�Ʈ
    // ���� ������ ���� ���� �󿡼��� ����
    [SerializeField] private float costIncreaseInterval = 1f; // �ڽ�Ʈ ȸ�� �ӵ��� ����
    [SerializeField] private int maxDeploymentCost = 99;
    [SerializeField] private int _currentDeploymentCost = 10;
    private float currentCostGauge = 0f;

    // ���� ���� ����
    public int TotalEnemyCount { get; private set; }
    public int MaxLifePoints { get; private set; } = 3;
    public int PassedEnemies { get; private set; } = 0;


    private int _killedEnemyCount;
    private int _currentLifePoints;
    public int KilledEnemyCount
    {
        get => _killedEnemyCount;
        private set
        {
            if (_killedEnemyCount != value)
            {
                _killedEnemyCount = value;
                OnLifePointsChanged?.Invoke(_killedEnemyCount);
            }
        }
    }
    public int CurrentLifePoints
    {
        get => _currentLifePoints;
        private set
        {
            if (_currentLifePoints != value)
            {
                _currentLifePoints = value;
                OnLifePointsChanged?.Invoke(_currentLifePoints);
            }
        }
    }
    public bool IsSpeedUp { get; private set; } = false;

    private const float speedUpScale = 2f;
    private const float originalTimeScale = 1f;
    private const float placementTimeScale = 0.2f;

    public int CurrentDeploymentCost
    {
        get => _currentDeploymentCost;
        private set
        {
            // ���ʿ��� ������Ʈ ���� - ���� ����� ���� �ڵ尡 ����ȴ�
            if (_currentDeploymentCost != value)
            {
                _currentDeploymentCost = value;
                OnDeploymentCostChanged?.Invoke(); // ���� ����� �� �̺�Ʈ �߻�. 
            }
        }
    }
    public float CurrentCostGauge => currentCostGauge;
    // �̺�Ʈ
    public event System.Action OnDeploymentCostChanged; // �̺�Ʈ �ߵ� ������ currentDeploymentCost ���� ���� ��, ���� ��ϵ� �Լ����� ����
    public event System.Action<int> OnLifePointsChanged; // ������ ����Ʈ ���� �� �߻� �̺�Ʈ
    public event System.Action OnEnemyKilled; // ���� ���� ������ �߻� �̺�Ʈ
    public event System.Action OnPreparationComplete; // �������� �غ� �Ϸ� �̺�Ʈ 

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
            // StageLoader���� �������� ������ ó����
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

        // ���� �ʱ�ȭ
        TotalEnemyCount = CalculateTotalEnemyCount();
        KilledEnemyCount = 0;
        CurrentLifePoints = MaxLifePoints;

        UIManager.Instance.InitializeUI();

        OnPreparationComplete?.Invoke();
    }

    public void StartStage()
    {
        Debug.Log("�������� ����");
        SetGameState(GameState.Battle);
        StartCoroutine(IncreaseCostOverTime());
        SpawnerManager.Instance.StartSpawning();
    }

    private int CalculateTotalEnemyCount()
    {
        int count = 0;
        EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>(); // ���� �ʿ�) Map �����տ��� �����ʵ� ���� �������� ������
        foreach (var spawner in spawners)
        {
            count += spawner.enemySpawnList.Count;
        }
        return count;
    }

    public void SetGameState(GameState gameState)
    {
        currentState = gameState;

        switch (gameState)
        {
            case GameState.Battle:
                Time.timeScale = IsSpeedUp ? speedUpScale : originalTimeScale;
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

    private IEnumerator IncreaseCostOverTime()
    {
        while (currentState == GameState.Battle)
        {
            yield return null; // �� �����Ӹ��� ����

            currentCostGauge += Time.deltaTime / costIncreaseInterval;

            if (currentCostGauge >= 1f)
            {
                currentCostGauge -= 1f;
                if (_currentDeploymentCost < maxDeploymentCost)
                {
                    CurrentDeploymentCost++;
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

        KilledEnemyCount++;
        UIManager.Instance.UpdateEnemyKillCountText();

        // ��� "������" ���� �����ϸ� ������ ���� �� �ٸ��� ��� ��
        if (KilledEnemyCount + PassedEnemies >= TotalEnemyCount)
        {
            GameWin();
        }
    }

    public void OnEnemyReachDestination()
    {
        if (currentState == GameState.GameOver || currentState == GameState.GameWin) return; 

        CurrentLifePoints--; // OnLifePointsChanged : ������� ���̸� �ڵ����� UI ������Ʈ �߻�
        PassedEnemies++;

        if (CurrentLifePoints <= 0)
        {
            GameOver();
        }
        else if (KilledEnemyCount + PassedEnemies >= TotalEnemyCount)
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

    public void ReturnToMainMenu(bool isPerfectClear = false)
    {
        if (isPerfectClear)
        {
            GameManagement.Instance.StageLoader.ReturnToMainMenu();
        }
        else
        {
            GameManagement.Instance.StageLoader.ReturnToMainMenuWithStageSelected();
        }
    }

    public void SlowDownTime()
    {
        Time.timeScale = placementTimeScale;
    }

    public void UpdateTimeScale()
    {
        if (currentState != GameState.Paused)
        {
            Time.timeScale = IsSpeedUp ? speedUpScale : originalTimeScale;
        }
    }

    public void ToggleSpeedUp()
    {
        if (currentState != GameState.Battle) return;

        IsSpeedUp = !IsSpeedUp;
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

    public void SetStageData(StageData data)
    {
        stageData = data;
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