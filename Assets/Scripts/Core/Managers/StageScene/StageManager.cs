using UnityEngine;
using System.Collections; // IEnumerator - �ڷ�ƾ���� �ַ� ����ϴ� ����
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System;
using Skills.Base;

// �������� ������ ���������� ���õ� ���� ���µ��� �����մϴ�.
public class StageManager : MonoBehaviour
{
    public static StageManager? Instance { get; private set; }
    public GameState currentState;
    private StageData? stageData;
    public StageData? StageData => stageData;

    private Map? currentMap;

    // ��ġ �ڽ�Ʈ
    private float timeToFillCost = 0.5f; // �ڽ�Ʈ 1 ȸ���� �ɸ��� �ð�
    private int currentDeploymentCost;
    private int maxDeploymentCost;
    private float currentCostGauge = 0f;

    // ���� ���� ����
    public int TotalEnemyCount { get; private set; }
    public int MaxLifePoints { get; private set; } = 3;

    private int _passedEnemies = 0;
    public int PassedEnemies
    {
        get => _passedEnemies;
        private set
        {
            if (_passedEnemies != value)
            {
                _passedEnemies = value;
                OnEnemyPassed?.Invoke(_passedEnemies);
            }
        }
    }


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

    // �гο� �����ϱ� ���� ���� ����
    public IReadOnlyList<ItemWithCount> FirstClearRewards = default!;
    public IReadOnlyList<ItemWithCount> BasicClearRewards = default!;



    // �̺�Ʈ
    public event Action<Map>? OnMapLoaded;
    public event Action OnStageStarted; // GameState.Battle�� ���ʷ� ������� ��
    public event Action<bool> OnSpeedUpChanged; // ��� ��ȭ �߻�
    public event Action? OnDeploymentCostChanged; // �̺�Ʈ �ߵ� ������ currentDeploymentCost ���� ���� ��, ���� ��ϵ� �Լ����� ����
    public event Action<int>? OnEnemyPassed; // ���� ���� ������ �������� �� �߻� �̺�Ʈ
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

        Enemy.OnEnemyDespawned += HandleEnemyDespawned;
        OnGameEnded += ClearStageObjectPools;
    }

    private void Start()
    {
        // Awake���� UIManager null ������ ���ڱ� ���� Start�� �Űܳ���
        StageUIManager.Instance!.UpdateSpeedUpButtonVisual(StageManager.Instance!.IsSpeedUp);
        StageUIManager.Instance!.UpdatePauseButtonVisual();

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

    public void InitializeStage(StageData stageData, List<SquadOperatorInfo> squadData, StageLoadingScreen stageLoadingScreen)
    {
        this.stageData = stageData;
        this.stageLoadingScreen = stageLoadingScreen;

        // �� �غ�
        InitializeMap();

        // ���������� �ʿ��� ��� ���(������Ʈ Ǯ)�� �غ���
        PreloadStageObjectPools(squadData);

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
        timeToFillCost *= stageData!.costPerSecondModifier;

        // ü�� ����Ʈ �ʱ�ȭ
        CurrentLifePoints = MaxLifePoints;

        // UI �ʱ�ȭ
        StageUIManager.Instance!.Initialize();
         
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

        count += spawners! // spawners�� null�� �ƴ��� ����
            .Sum(spawner =>
            spawner.spawnList?.spawnedEnemies?.Count(spawnInfo => spawnInfo.spawnType is SpawnType.Enemy) 
            ?? 0 // spawnList�� spawnedEnemies�� null�̸� 0�� ���
            );
        

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

        StageUIManager.Instance!.UpdatePauseButtonVisual();
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

    private void HandleEnemyDespawned(Enemy enemy, DespawnReason reason)
    {
        switch (reason)
        {
            case DespawnReason.Defeated:
                OnEnemyDefeated(enemy);
                break;
            case DespawnReason.ReachedGoal:
                OnEnemyReachDestination(enemy);
                break;
            default:
                Debug.LogError("ó���Ǹ� �ȵǴ� ��?");
                break;
        }
    }

    // Enemy�� ������ ������ ȣ��
    public void OnEnemyDefeated(Enemy enemy)
    {
        KilledEnemyCount++;
        StageUIManager.Instance!.UpdateEnemyKillCountText();

        // ��� "������" ���� �����ϸ� ������ ���� �� �ٸ��� ��� ��
        // ������ ������ ���� ���� ������ �̷��� ����.
        if (KilledEnemyCount + PassedEnemies >= TotalEnemyCount)
        {
            StartCoroutine(GameWinAfterDelay());
        }
    }

    private IEnumerator GameWinAfterDelay()
    {
        yield return new WaitForSeconds(0.22f); // ���� ������� �ð��� 0.2�ʴϱ� �װź��� ���� �� ���
        GameWin();
    }

    private IEnumerator GameOverAfterDelay()
    {
        yield return new WaitForSeconds(0.22f); // ���� ������� �ð��� 0.2�ʴϱ� �װź��� ���� �� ���
        GameOver();
    }

    public void OnEnemyReachDestination(Enemy enemy)
    {
        if (currentState == GameState.Battle)
        {
            CurrentLifePoints -= enemy.BaseData.PlayerDamage; // OnLifePointsChanged : ������� ���̸� �ڵ����� UI ������Ʈ �߻�
            PassedEnemies += enemy.BaseData.PlayerDamage; 

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

        // ������ ���� ���� �� ����
        // RewardManager.SetAndGrantStageRewards�� ����������, SetStageRewards���� ��ȯ�Ǵ� ���� StageManager���� ���� �־�� �Ѵٴ� �������� �ִ�.
        (FirstClearRewards, BasicClearRewards) = GameManagement.Instance!.RewardManager.SetStageRewards(stageData, stars);
        List<ItemWithCount> firstClearRewards = new List<ItemWithCount>(FirstClearRewards);
        List<ItemWithCount> basicClearRewards = new List<ItemWithCount>(BasicClearRewards);
        GameManagement.Instance!.PlayerDataManager.GrantStageRewards(firstClearRewards, basicClearRewards);

        GameManagement.Instance!.PlayerDataManager.RecordStageResult(stageData!.stageId, stars);

        StageUIManager.Instance!.HidePauseOverlay();
        StageUIManager.Instance!.ShowGameWinUI(stars);

        OnGameEnded?.Invoke();
        OnGameCleared?.Invoke();
        StopAllCoroutines();
    }

    private void GameOver()
    { 
        SetGameState(GameState.GameOver);
        StageUIManager.Instance!.ShowGameOverUI();
        OnGameEnded?.Invoke();
        OnGameFailed?.Invoke();
        StopAllCoroutines();
    }

    // ������ ������������ ���������� ��ư�� ������ �� ����
    public void RequestExit()
    {
        // ������ ���� ������ �ߺ� ȣ�� ����
        if (currentState == GameState.GameWin || currentState == GameState.GameOver) return;

        // GameOver�� ����
        GameOver();

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
            StageUIManager.Instance!.HidePauseOverlay();
        }
        else if (currentState == GameState.Battle)
        {
            SetGameState(GameState.Paused);
            StageUIManager.Instance!.ShowPauseOverlay();
        }
        StageUIManager.Instance!.UpdatePauseButtonVisual();
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
    private void PrepareDeployables(List<SquadOperatorInfo> squadData)
    {
        InstanceValidator.ValidateInstance(StageData);

        // �ʿ��� ��ġ ������ ��Ҹ� ������. ������ �� ����Ʈ
        var mapDeployables = StageData!.mapDeployables ?? new List<MapDeployableData>();

        // �����ؼ� ���
        var deployableList = new List<MapDeployableData>(mapDeployables);

        // ������ + ���� ��ġ ���� ��� �ʱ�ȭ
        DeployableManager.Instance!.Initialize(squadData, deployableList, stageData!.operatorMaxDeploymentCount);
    }


    // �������� ���� �ÿ� ��� ������Ʈ Ǯ�� �ı��մϴ�.
    private void ClearStageObjectPools()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ClearAllPools();
        }
    }

    private void PreloadStageObjectPools(List<SquadOperatorInfo> squadData)
    {
        if (ObjectPoolManager.Instance == null)
        {
            Debug.LogError("ObjectPoolManager�� ���� Ǯ ���� �Ұ���");
            return;
        }

        // 1. ������ ��� �� ���� ��� Ȯ��
        var uniqueEnemyPrefabs = new HashSet<GameObject>();

        // ������ ����
        if (currentMap != null)
        {
            foreach (var spawner in currentMap.EnemySpawners)
            {
                if (spawner.spawnList == null) continue;
                foreach (var spawnInfo in spawner.spawnList.spawnedEnemies)
                {
                    if (spawnInfo.prefab != null)
                    {
                        uniqueEnemyPrefabs.Add(spawnInfo.prefab);
                    }
                }
            }
        }
        // Ǯ ����
        foreach (var enemyPrefab in uniqueEnemyPrefabs)
        {
            Enemy enemy = enemyPrefab.GetComponent<Enemy>();
            if (enemy == null || enemy.BaseData == null) continue;
            EnemyData enemyData = enemy.BaseData;

            string enemyPoolTag = enemyData.GetUnitTag();
            ObjectPoolManager.Instance.CreatePool(enemyPoolTag, enemyPrefab, 10);
            Debug.Log($"enemy Ǯ - {enemyPoolTag} ������");

        }

        // 2. �������� ��� ��ġ ���� ��� ���� ��� Ȯ�� �� Ǯ�� (������ ���� + Ǯ ����)
        // a. ������
        foreach (var opInfo in squadData)
        {
            if (opInfo.op.OperatorProgressData != null)
            {
                OperatorData opData = opInfo.op.OperatorProgressData;
                GameObject operatorPrefab = opData.prefab;

                string opPoolTag = opData.GetUnitTag();
                ObjectPoolManager.Instance.CreatePool(opPoolTag, operatorPrefab, 1);
                Debug.Log($"operator Ǯ - {opPoolTag} ������");
            }
        }
        // b. �� ����
        if (stageData != null && stageData.mapDeployables != null)
        {
            foreach (var mapDeployableData in stageData.mapDeployables)
            {
                if (mapDeployableData != null)
                {
                    DeployableUnitData deployableData = mapDeployableData.DeployableData;
                    GameObject deployablePrefab = deployableData.prefab;

                    string deployablePoolTag = deployableData.GetUnitTag();
                    ObjectPoolManager.Instance.CreatePool(deployablePoolTag, deployablePrefab, 1);
                    Debug.Log($"operator Ǯ - {deployablePoolTag} ������");
                }
            }
        }

        // 3. ���� ������Ʈ Ǯ��
        // 3-a. Enemy�� ���� ������Ʈ
        foreach (var enemyPrefab in uniqueEnemyPrefabs)
        {
            Enemy enemyComponent = enemyPrefab.GetComponent<Enemy>();
            if (enemyComponent == null || enemyComponent.BaseData == null) continue;

            EnemyData enemyBaseData = enemyComponent.BaseData;
            enemyBaseData.CreateObjectPools();
            Debug.Log($"{enemyBaseData} ���� ���� ������Ʈ Ǯ ������");
        }

        // 3-b. Opereator�� ���� ������Ʈ
        foreach (var opInfo in squadData)
        {
            OperatorData opData = opInfo.op.OperatorProgressData;
            if (opData == null) continue;
            opData.CreateObjectPools();

            // ��ų ������Ʈ Ǯ ����
            // "���õ� ��ų"�̶�� ������ ���⳪ ������ �������� �����ǹǷ� ���⼭ ����
            int skillIndex = opInfo.skillIndex;
            if (skillIndex >= 0 && skillIndex < 2)
            {
                // �ε����� 0 or 1
                OperatorSkill selectedSkill = skillIndex == 0 ? opData.elite0Skill : opData.elite1Unlocks.unlockedSkill;
                if (selectedSkill != null)
                {
                    selectedSkill.PreloadObjectPools(opData);
                }
            }

            Debug.Log($"{opData} ���� ���� ������Ʈ Ǯ ������");
        }

        // 3-c. �� ���� ��ġ ������ ���� ������Ʈ Ǯ��
        if (stageData != null && stageData.mapDeployables != null)
        {
            foreach (var mapDeployableData in stageData.mapDeployables)
            {
                if (mapDeployableData.DeployableData == null) continue;

                DeployableUnitData deployableData = mapDeployableData.DeployableData;
                if (deployableData != null)
                {
                    deployableData.CreateObjectPools();
                    Debug.Log($"{deployableData} ���� ���� ������Ʈ Ǯ ������");
                }
            }
        }
    }

    private void OnDestroy()
    {
        stageLoadingScreen!.OnHideComplete -= StartStageCoroutine;
        Enemy.OnEnemyDespawned -= HandleEnemyDespawned;
        OnGameEnded -= ClearStageObjectPools;
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