using UnityEngine;
using System.Collections; // IEnumerator - �ڷ�ƾ���� �ַ� ����ϴ� ����
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System;

// �������� ������ ���������� ���õ� ���� ���µ��� �����մϴ�.
public class StageManager : MonoBehaviour
{
    public static StageManager? Instance { get; private set; }
    public GameState currentState;
    private StageData? stageData;
    public StageData? StageData => stageData;

    private Map? currentMap;

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
    private bool isSpeedUp = false; 
    public bool IsSpeedUp
    {
        get => isSpeedUp;
        set
        {
            if (isSpeedUp != value)
            {
                isSpeedUp = value;
                // �ð� ��ȭ �̺�Ʈ �߻�
                OnSpeedUpChanged?.Invoke(isSpeedUp);
            }
        }
    }

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
    private Coroutine? costIncreaseCoroutine;
    private float lastCostUpdateTime;
    private const float COST_CHECK_INTERVAL = 1f;

    // ��ũ���� ������ ����� ���� ���� ������ ���� �Ҵ�
    private StageLoadingScreen? stageLoadingScreen;

    private List<ItemWithCount> actualFirstClearRewards;
    public IReadOnlyList<ItemWithCount> ActualFirstClearRewards => actualFirstClearRewards;
    private List<ItemWithCount> actualBasicClearRewards;
    public IReadOnlyList<ItemWithCount> ActualBasicClearRewards => actualBasicClearRewards;


    // �̺�Ʈ
    public event Action<Map>? OnMapLoaded;
    public event Action OnStageStarted; // GameState.Battle�� ���ʷ� ������� ��
    public event Action<bool> OnSpeedUpChanged; // ��� ��ȭ �߻�
    public event Action? OnDeploymentCostChanged; // �̺�Ʈ �ߵ� ������ currentDeploymentCost ���� ���� ��, ���� ��ϵ� �Լ����� ����
    public event Action<int>? OnLifePointsChanged; // ������ ����Ʈ ���� �� �߻� �̺�Ʈ
    public event Action? OnEnemyKilled; // ���� ���� ������ �߻� �̺�Ʈ
    public event Action? OnPreparationCompleted; // �������� �غ� �Ϸ� �̺�Ʈ 
    public event Action<GameState>? OnGameStateChanged;
    public event Action? OnGameCleared;
    public event Action? OnGameFailed;

    public event Action? OnGameEnded; // ���� ���� �ÿ� ���� - ����, ���� ����

    private void Awake()
    {
        // �̱��� ����
       if (Instance! == null)
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
        UIManager.Instance!.UpdateSpeedUpButtonVisual(StageManager.Instance!.IsSpeedUp);
        UIManager.Instance!.UpdatePauseButtonVisual();

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
        InstanceValidator.ValidateInstance(stageData);

        SetGameState(GameState.Preparation);

        // ���� ����

        // ���� ���� �ʱ�ȭ
        TotalEnemyCount = CalculateTotalEnemyCount();
        KilledEnemyCount = 0;

        // �ڽ�Ʈ ���� �ʱ�ȭ
        currentDeploymentCost = stageData!.startDeploymentCost;
        maxDeploymentCost = stageData!.maxDeploymentCost;
        timeToFillCost = stageData!.timeToFillCost;

        // ü�� ����Ʈ �ʱ�ȭ
        CurrentLifePoints = MaxLifePoints;

        // UI �ʱ�ȭ
        UIManager.Instance!.Initialize();
         
        OnPreparationCompleted?.Invoke();
    }

    private void StartStageCoroutine()
    {
        StartCoroutine(StartStageWithDelay());
    }

    private IEnumerator StartStageWithDelay()
    {
        yield return new WaitForSecondsRealtime(0.5f); // Time.timeScale�� ������ ���� �ʰ� ����

        if (SpawnerManager.Instance! == null) throw new InvalidOperationException("������ �Ŵ��� �ν��Ͻ��� ����");

        SetGameState(GameState.Battle);
        lastCostUpdateTime = Time.time;
        CheckTutorial();
        StartCoroutine(IncreaseCostOverTime());
        SpawnerManager.Instance!.StartSpawning();
    }

    private void CheckTutorial()
    {
        if (stageData.stageId == "1-0")
        {
            bool secondTutorialNotStarted = GameManagement.Instance!.PlayerDataManager.IsTutorialStatus(1, PlayerDataManager.TutorialStatus.NotStarted);
            bool secondTutorialInProgress = GameManagement.Instance!.PlayerDataManager.IsTutorialStatus(1, PlayerDataManager.TutorialStatus.InProgress);
            bool secondTutorialFailed = GameManagement.Instance!.PlayerDataManager.IsTutorialStatus(1, PlayerDataManager.TutorialStatus.Failed);

            if (secondTutorialNotStarted)
            {
                GameManagement.Instance!.TutorialManager.StartSecondTutorial();
                return;
            }
            else if (secondTutorialInProgress || secondTutorialFailed)
            {
                GameManagement.Instance!.TutorialManager.StartSecondTutorialQuiet();
            }
        }
    }


    private int CalculateTotalEnemyCount()
    {
        InstanceValidator.ValidateInstance(currentMap);

        int count = 0;
        IReadOnlyList<EnemySpawner> spawners = currentMap!.EnemySpawners;
        //InstanceValidator.ValidateInstance(spawners);

        foreach (var spawner in spawners!)
        {
            // Enemy�� ��ü ������ ���
            count += spawner.spawnList
                .Count(spawnInfo => spawnInfo.spawnType is SpawnType.Enemy);
        }
        return count;
    }

    // ������ ���¸� �����ϰ� �׿� �´� �ð� �ӵ� ����
    public void SetGameState(GameState gameState)
    {
        currentState = gameState;
        TimeManager timeManager = GameManagement.Instance!.TimeManager;

        switch (gameState)
        {
            case GameState.Battle:
                timeManager.UpdateTimeScale(IsSpeedUp);
                break;

            case GameState.Paused:
            case GameState.GameOver:
            case GameState.GameWin:
                timeManager.SetPauseTime();
                break;
        }

        UIManager.Instance!.UpdatePauseButtonVisual();
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
        UIManager.Instance!.UpdateEnemyKillCountText();

        // ��� "������" ���� �����ϸ� ������ ���� �� �ٸ��� ��� ��
        if (KilledEnemyCount + PassedEnemies >= TotalEnemyCount)
        {
            StartCoroutine(GameWinAfterDelay());
        }
    }

    private IEnumerator GameWinAfterDelay()
    {
        yield return null;
        GameWin();
    }

    private IEnumerator GameOverAfterDelay()
    {
        yield return null;
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
        SetGameState(GameState.GameWin);
        int stars = 3 - PassedEnemies;

        SetStageRewards(stageData, stars);
        GameManagement.Instance!.PlayerDataManager.GrantStageRewards();
        GameManagement.Instance!.PlayerDataManager.RecordStageResult(stageData!.stageId, stars);

        UIManager.Instance!.HidePauseOverlay();
        UIManager.Instance!.ShowGameWinUI(stars);

        OnGameEnded?.Invoke();
        OnGameCleared?.Invoke();
        StopAllCoroutines();
    }

    private void GameOver()
    { 
        SetGameState(GameState.GameOver);
        UIManager.Instance!.ShowGameOverUI();
        OnGameEnded?.Invoke();
        OnGameFailed?.Invoke();
        StopAllCoroutines();
    }

    // ������ ������������ ���������� ��ư�� ������ �� ����
    public void RequestExit()
    {
        SetGameState(GameState.GameOver);
        StopAllCoroutines();
        StartCoroutine(UIManager.Instance!.ShowResultAfterDelay(0));
    }

    public void ReturnToMainMenu(bool isPerfectClear = false)
    {
        if (GameManagement.Instance! == null) throw new InvalidOperationException("GameManagement �ʱ�ȭ �ȵ�");

        if (isPerfectClear)
        {
            GameManagement.Instance!.StageLoader.ReturnToMainMenu();
        }
        else
        {
            GameManagement.Instance!.StageLoader.ReturnToMainMenuWithStageSelected();
        }
    }

    public void ToggleSpeedUp()
    {
        if (currentState != GameState.Battle) return;

        IsSpeedUp = !IsSpeedUp;
        GameManagement.Instance!.TimeManager.UpdateTimeScale(IsSpeedUp);
    }

    public void TogglePause()
    {
        Debug.Log("Pause ����");

        if (currentState == GameState.GameOver || currentState == GameState.GameWin) return;

        if (currentState == GameState.Paused)
        {
            SetGameState(GameState.Battle);
            UIManager.Instance!.HidePauseOverlay();
        }
        else if (currentState == GameState.Battle)
        {
            SetGameState(GameState.Paused);
            UIManager.Instance!.ShowPauseOverlay();
        }
        UIManager.Instance!.UpdatePauseButtonVisual();
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
        InstanceValidator.ValidateInstance(stageData);

        if (stageData!.mapPrefab != null)
        {
            try
            {
                GameObject mapObject = Instantiate(stageData!.mapPrefab);
                currentMap = mapObject.GetComponent<Map>();

                if (currentMap == null || currentMap.Mapid != stageData!.stageId)
                {
                    Debug.LogError("�� ID�� �������� ������ ��ġ���� �ʽ��ϴ�!");
                    return;
                }

                mapObject.name = "Map";
                mapObject.transform.SetParent(MapManager.Instance!.transform);

                // ��ġ �ʱ�ȭ
                mapObject.transform.localPosition = Vector3.zero;
                mapObject.transform.localRotation = Quaternion.identity;

                MapManager.Instance!.InitializeMap(currentMap);
                OnMapLoaded?.Invoke(currentMap);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"�� �ʱ�ȭ �� ���� �߻� : {e.Message}");
                return;
            }
            
        }
    }

    // ��ġ ������ ���� ����Ʈ�� �غ��մϴ�. ���۷����� + ������������ ��� ������ ������Ʈ
    private void PrepareDeployables(List<OwnedOperator> squadData)
    {
        InstanceValidator.ValidateInstance(StageData);

        // �ʿ��� ��ġ ������ ��Ҹ� ������. ������ �� ����Ʈ
        var mapDeployables = StageData!.mapDeployables ?? new List<MapDeployableData>();

        // �����ؼ� ���
        var deployableList = new List<MapDeployableData>(mapDeployables);

        // ������ + ���� ��ġ ���� ��� �ʱ�ȭ
        DeployableManager.Instance!.Initialize(squadData, deployableList, stageData!.operatorMaxDeploymentCount);
    }

    // Ŭ���� �� ������ ���� ������ �����մϴ�. 
    public void SetStageRewards(StageData stageData, int stars)
    {
        var stageResultInfo = GameManagement.Instance!.PlayerDataManager.GetStageResultInfo(stageData.stageId);
        List<ItemWithCount> perfectFirstClearRewards = stageData.FirstClearRewardItems;
        List<ItemWithCount> perfectBasicClearRewards = stageData.BasicClearRewardItems;

        // ���� Ŭ���� ����. ���� ������ �޼��� ���ο� ����
        float firstClearExpItemRate = SetFirstClearExpItemRate(stageResultInfo, stars);
        float firstClearPromoItemRate = SetFirstClearPromotionItemRate(stageResultInfo, stars);
        actualFirstClearRewards = MultiplyRewards(perfectFirstClearRewards, firstClearExpItemRate, firstClearPromoItemRate);


        float basicClearExpItemRate = SetBasicClearItemRate(stars);
        actualBasicClearRewards = MultiplyRewards(perfectBasicClearRewards, basicClearExpItemRate);
    }

    // �� reward�� count�� itemRate�� ���Ͽ� �� ����Ʈ�� ��ȯ�մϴ�.
    private List<ItemWithCount> MultiplyRewards(List<ItemWithCount> rewards, float expItemRate, float promoItemRate = 0f)
    {
        List<ItemWithCount> scaledRewards = new List<ItemWithCount>();

        // 3�� Ŭ��� �ݺ����� ��� ������ 0�� �� ������, �� ���� �� ����Ʈ�� ��ȯ�� 
        if (expItemRate == 0f && promoItemRate == 0f) return scaledRewards; 

        foreach (var reward in rewards)
        {
            // ����ȭ ������ ó��
            if (reward.itemData.type == ItemData.ItemType.EliteItem)
            {
                int scaledCount = Mathf.FloorToInt(reward.count * promoItemRate);
                scaledRewards.Add(new ItemWithCount(reward.itemData, scaledCount));
            }

            // ����ġ ������ ó��
            else
            {
                int scaledCount = Mathf.FloorToInt(reward.count * expItemRate);
                // ItemWithCount ��ü�� ���� �����մϴ�.
                scaledRewards.Add(new ItemWithCount(reward.itemData, scaledCount));
            }
        }
        return scaledRewards;
    }

    // n���� ���ʷ� �޼����� ���� ����ġ ������ ���� ������ ����Ѵ�.
    private float SetFirstClearExpItemRate(StageResultData.StageResultInfo? resultInfo, int stars)
    {
        // Ŭ������ �� ���� ���� ���� ���� ����
        if (resultInfo == null)
        {
            if (stars == 1) return 0.25f;
            else if (stars == 2) return 0.5f;
            else if (stars == 3) return 1f;
        }

        // ������ 3������ Ŭ�����ߴٸ� ���� ������ ����
        if (resultInfo.stars == 3) return 0f;

        // �������� �� �� Ŭ�������� �� - ���� 2, 3���� ������ ������
        if (resultInfo.stars < stars)
        {
            if (resultInfo.stars == 1)
            {
                if (stars == 2) return 0.25f;
                if (stars == 3) return 0.75f;
            }
            else if (resultInfo.stars == 2)
            {
                if (stars == 3) return 0.5f;
            }
        }

        // return ������ ���������� ���ߴٸ� ���� �߻�
        throw new InvalidOperationException("FirstClearItemRate�� ����ġ ���� ����");
    }

    private float SetFirstClearPromotionItemRate(StageResultData.StageResultInfo? resultInfo, int stars)
    {
        // ������ Ŭ������ ���� ���� 3�� Ŭ������ ��쿡�� 1f ��ȯ�Ͽ� ����ȭ ������ ����
        if (resultInfo == null && stars == 3)
        {
            return 1f;
        }

        // ������ ���� 0 ��ȯ
        return 0f;
    }

    // n������ Ŭ�������� ���� ������ ���� ������ ����Ѵ�
    private float SetBasicClearItemRate(int stars)
    {
        if (stars == 1) return 0.25f;
        else if (stars == 2) return 0.5f;
        else if (stars == 3) return 1f;
        else throw new InvalidOperationException("BasicClearItemRate�� ����ġ ���� ����");
    }

    private void OnDestroy()
    {
        stageLoadingScreen!.OnHideComplete -= StartStageCoroutine;
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