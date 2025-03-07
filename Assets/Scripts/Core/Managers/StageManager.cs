using UnityEngine;
using System.Collections; // IEnumerator - �ڷ�ƾ���� �ַ� ����ϴ� ����
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System;

// �������� ������ ���������� ���õ� ���� ���µ��� �����մϴ�.
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    public GameState currentState;
    private StageData stageData;
    public StageData StageData => stageData;

    private Map currentMap;

    // ��ġ �ڽ�Ʈ
    private float timeToFillCost; // �ڽ�Ʈ 1 ȸ���� �ɸ��� �ð�
    private int currentDeploymentCost;
    private int maxDeploymentCost;
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
                OnEnemyKilled?.Invoke();
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

    private float gameEndDelay = 0.2f; // ���� ���� ���� �޼� �� ��ٸ��� �ð�

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

    // �ڷ�ƾ ���ߴ� ���� ��� 
    private Coroutine costIncreaseCoroutine;
    private float lastCostUpdateTime;
    private const float COST_CHECK_INTERVAL = 1f;

    // ��ũ���� ������ ����� ���� ���� ������ ���� �Ҵ�
    StageLoadingScreen stageLoadingScreen;

    // �̺�Ʈ
    public event System.Action<Map> OnMapLoaded;
    public event System.Action OnDeploymentCostChanged; // �̺�Ʈ �ߵ� ������ currentDeploymentCost ���� ���� ��, ���� ��ϵ� �Լ����� ����
    public event System.Action<int> OnLifePointsChanged; // ������ ����Ʈ ���� �� �߻� �̺�Ʈ
    public event System.Action OnEnemyKilled; // ���� ���� ������ �߻� �̺�Ʈ
    public event System.Action OnPreparationCompleted; // �������� �غ� �Ϸ� �̺�Ʈ 
    public event System.Action<GameState> OnGameStateChanged;
    public event System.Action OnGameEnded; // ���� ���� �ÿ� ����

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

        if (GameManagement.Instance != null)
        {
            // StageLoader���� �������� ������ ó����
            return;
        }
    }

    private void Update()
    {
        if (currentState == GameState.Battle)
        {
            if (Time.time - lastCostUpdateTime > COST_CHECK_INTERVAL)
            {
                Debug.LogWarning("1�� ���� �ڽ�Ʈ ������Ʈ�� �������� ����, �ڽ�Ʈ ȸ�� �����");
                RestartCostIncrease();
            }
        }
    }

    public void InitializeStage(StageData stageData, List<OwnedOperator> squadData, StageLoadingScreen stageLoadingScreen)
    {
        this.stageData = stageData;
        this.stageLoadingScreen = stageLoadingScreen;

        // �� �غ�
        InitializeMap();
        
        // �ʿ��� ������ �� �־ �� �ʱ�ȭ �Ŀ� �����ؾ� ��
        PrepareDeployables(squadData);

        // �������� �غ�
        PrepareStage();

        // �ε� ȭ���� ����� �Ŀ� StartStage�� ������
        stageLoadingScreen.OnHideComplete += StartStageCoroutine;
    }

    private void PrepareStage()
    {
        SetGameState(GameState.Preparation);

        // ���� ����

        // ���� ���� �ʱ�ȭ
        TotalEnemyCount = CalculateTotalEnemyCount();
        KilledEnemyCount = 0;

        // �ڽ�Ʈ ���� �ʱ�ȭ
        currentDeploymentCost = stageData.startDeploymentCost;
        maxDeploymentCost = stageData.maxDeploymentCost;
        timeToFillCost = stageData.timeToFillCost;

        // ü�� ����Ʈ �ʱ�ȭ
        CurrentLifePoints = MaxLifePoints;

        // UI �ʱ�ȭ
        UIManager.Instance.Initialize();
         
        OnPreparationCompleted?.Invoke();
    }

    private void StartStageCoroutine()
    {
        StartCoroutine(StartStageWithDelay());
    }

    private IEnumerator StartStageWithDelay()
    {
        yield return new WaitForSecondsRealtime(0.5f); // Time.timeScale�� ������ ���� �ʰ� ����

        if (SpawnerManager.Instance == null) throw new InvalidOperationException("������ �Ŵ��� �ν��Ͻ��� ����");


        SetGameState(GameState.Battle);
        lastCostUpdateTime = Time.time;
        StartCoroutine(IncreaseCostOverTime());
        SpawnerManager.Instance.StartSpawning();
    }

    private int CalculateTotalEnemyCount()
    {
        int count = 0;
        //EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>(); // ���� �ʿ�) Map �����տ��� �����ʵ� ���� �������� ������
        IReadOnlyList<EnemySpawner> spawners = currentMap.EnemySpawners;
        foreach (var spawner in spawners)
        {
            // Enemy�� ��ü ������ ���
            count += spawner.spawnList
                .Count(spawnInfo => spawnInfo.spawnType is SpawnType.Enemy);
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
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
            case GameState.GameWin:
                Time.timeScale = 0f;
                break;
        }

        UIManager.Instance.UpdatePauseButtonVisual();
        OnGameStateChanged?.Invoke(gameState);
        // �ٸ� �ʿ��� ���� ���� ����...
    }

    private IEnumerator IncreaseCostOverTime()
    {
        while (true)
        {
            if (currentState == GameState.Battle)
            {
                yield return null; // �� �����Ӹ��� ����

                currentCostGauge += Time.deltaTime / timeToFillCost;
                lastCostUpdateTime = Time.time; 

                if (currentCostGauge >= 1f)
                {
                    currentCostGauge -= 1f;
                    if (currentDeploymentCost < maxDeploymentCost)
                    {
                        CurrentDeploymentCost++;
                    }
                }
            }

            // Pause, Battle�� �ƴ� �� �ڷ�ƾ ����
            if (currentState != GameState.Battle && currentState != GameState.Paused)
            {
                yield break; 
            }

            yield return null;
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

    // Enemy�� ������ ������ ȣ��
    public void OnEnemyDefeated()
    {
        KilledEnemyCount++;
        UIManager.Instance.UpdateEnemyKillCountText();

        // ��� "������" ���� �����ϸ� ������ ���� �� �ٸ��� ��� ��
        if (KilledEnemyCount + PassedEnemies >= TotalEnemyCount)
        {
            StartCoroutine(GameWinAfterDelay());
        }
    }

    private IEnumerator GameWinAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        GameWin();
    }

    private IEnumerator GameOverAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        GameOver();
    }

    public void OnEnemyReachDestination()
    {
        if (currentState == GameState.Battle)
        {
            CurrentLifePoints--; // OnLifePointsChanged : ������� ���̸� �ڵ����� UI ������Ʈ �߻�
            PassedEnemies++;

            if (CurrentLifePoints <= 0)
            {
                StartCoroutine(GameOverAfterDelay());
            }
            else if (KilledEnemyCount + PassedEnemies >= TotalEnemyCount)
            {
                StartCoroutine(GameWinAfterDelay());
            }
        }

    }

    private void GameWin()
    {
        if (GameManagement.Instance == null) throw new InvalidOperationException("GameManagement�� �ʱ�ȭ���� �ʾ���");

        SetGameState(GameState.GameWin);
        Time.timeScale = 0;
        int stars = 3 - PassedEnemies;
        UIManager.Instance.HidePauseOverlay();
        UIManager.Instance.ShowGameWinUI(stars);
        
        GameManagement.Instance.PlayerDataManager.RecordStageResult(stageData.stageId, stars);
        GameManagement.Instance.PlayerDataManager.GrantStageRewards(stageData.rewardItems);

        OnGameEnded?.Invoke();
        StopAllCoroutines();
    }

    private void GameOver()
    { 
        SetGameState(GameState.GameOver);
        Time.timeScale = 0; // ���� �Ͻ� ����
        UIManager.Instance.ShowGameOverUI();
        OnGameEnded?.Invoke();
        StopAllCoroutines();
    }

    // ������ ������������ ���������� ��ư�� ������ �� ����
    public void RequestExit()
    {
        SetGameState(GameState.GameOver);
        Time.timeScale = 0;
        StopAllCoroutines();
        StartCoroutine(UIManager.Instance.ShowResultAfterDelay(0));
    }

    public void ReturnToMainMenu(bool isPerfectClear = false)
    {
        if (GameManagement.Instance == null) throw new InvalidOperationException("GameManagement �ʱ�ȭ �ȵ�");

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
            UIManager.Instance.HidePauseOverlay();
        }
        else if (currentState == GameState.Battle)
        {
            SetGameState(GameState.Paused);
            UIManager.Instance.ShowPauseOverlay();
        }
        UIManager.Instance.UpdatePauseButtonVisual();
    }

    public void RecoverDeploymentCost(int amount)
    {
        CurrentDeploymentCost = Mathf.Min(CurrentDeploymentCost + amount, maxDeploymentCost);
    }

    private void RestartCostIncrease()
    {
        if (costIncreaseCoroutine != null)
        {
            StopCoroutine(costIncreaseCoroutine);
        }
        costIncreaseCoroutine = StartCoroutine(IncreaseCostOverTime());
        lastCostUpdateTime = Time.time; // ������� ����� ���
    }

    private void InitializeMap()
    {
        if (stageData.mapPrefab != null)
        {
            MapManager mapManager = MapManager.Instance;
            if (mapManager != null)
            {
                try
                {
                    GameObject mapObject = Instantiate(stageData.mapPrefab);
                    currentMap = mapObject.GetComponent<Map>();

                    if (currentMap == null || currentMap.Mapid != stageData.stageId)
                    {
                        Debug.LogError("�� ID�� �������� ������ ��ġ���� �ʽ��ϴ�!");
                        return;
                    }

                    mapObject.name = "Map";
                    mapObject.transform.SetParent(mapManager.transform);

                    // ��ġ �ʱ�ȭ
                    mapObject.transform.localPosition = Vector3.zero;
                    mapObject.transform.localRotation = Quaternion.identity;

                    mapManager.InitializeMap(currentMap);
                    OnMapLoaded?.Invoke(currentMap);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"�� �ʱ�ȭ �� ���� �߻� : {e.Message}");
                    return;
                }
            }
        }
    }

    // ��ġ ������ ���� ����Ʈ�� �غ��մϴ�. ���۷����� + ������������ ��� ������ ������Ʈ
    private void PrepareDeployables(List<OwnedOperator> squadData)
    {
        // �ʿ��� ��ġ ������ ��Ҹ� ������. ������ �� ����Ʈ
        var mapDeployables = StageData.mapDeployables ?? new List<MapDeployableData>();

        // �����ؼ� ���
        var deployableList = new List<MapDeployableData>(mapDeployables);

        // ������ + ���� ��ġ ���� ��� �ʱ�ȭ
        DeployableManager.Instance.Initialize(squadData, deployableList, stageData.operatorMaxDeploymentCount);
    }

    private void OnDestroy()
    {
        stageLoadingScreen.OnHideComplete -= StartStageCoroutine;
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