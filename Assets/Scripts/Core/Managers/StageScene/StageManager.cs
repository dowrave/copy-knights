using UnityEngine;
using System.Collections; // IEnumerator - 코루틴에서 주로 사용하는 버전
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System;
using Skills.Base;

// 스테이지 씬에서 스테이지와 관련된 여러 상태들을 관리합니다.
public class StageManager : MonoBehaviour
{
    public static StageManager? Instance { get; private set; }
    public GameState currentState;
    private StageData? stageData;
    public StageData? StageData => stageData;

    private Map? currentMap;

    // 배치 코스트
    private float timeToFillCost = 0.5f; // 코스트 1 회복에 걸리는 시간
    private int currentDeploymentCost;
    private int maxDeploymentCost;
    private float currentCostGauge = 0f;

    // 게임 상태 변수
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
                // 시간 변화 이벤트 발생
                OnSpeedUpChanged?.Invoke(isSpeedUp);
            }
        }
    }

    public int CurrentDeploymentCost
    {
        get => currentDeploymentCost;
        private set
        {
            // 불필요한 업데이트 방지 - 값이 변경될 때만 코드가 실행된다
            if (currentDeploymentCost != value)
            {
                currentDeploymentCost = value;
                OnDeploymentCostChanged?.Invoke(); // 값이 변경될 때 이벤트 발생. 
            }
        }
    }
    public float CurrentCostGauge => currentCostGauge;

    // 코루틴 멈추는 현상 대비 
    private Coroutine? costIncreaseCoroutine;
    private float lastCostUpdateTime;
    private const float COST_CHECK_INTERVAL = 1f;

    // 스크린이 완전히 사라진 다음 게임 시작을 위한 할당
    private StageLoadingScreen? stageLoadingScreen;

    // 패널에 전달하기 위한 보상 값들
    public IReadOnlyList<ItemWithCount> FirstClearRewards = default!;
    public IReadOnlyList<ItemWithCount> BasicClearRewards = default!;



    // 이벤트
    public event Action<Map>? OnMapLoaded;
    public event Action OnStageStarted; // GameState.Battle이 최초로 실행됐을 때
    public event Action<bool> OnSpeedUpChanged; // 배속 변화 발생
    public event Action? OnDeploymentCostChanged; // 이벤트 발동 조건은 currentDeploymentCost 값이 변할 때, 여기 등록된 함수들이 동작
    public event Action<int>? OnEnemyPassed; // 적이 도착 지점에 도달했을 때 발생 이벤트
    public event Action<int>? OnLifePointsChanged; // 라이프 포인트 변경 시 발생 이벤트
    public event Action? OnEnemyKilled; // 적을 잡을 때마다 발생 이벤트
    public event Action? OnPreparationCompleted; // 스테이지 준비 완료 이벤트 
    public event Action<GameState>? OnGameStateChanged;
    public event Action? OnGameCleared;
    public event Action? OnGameFailed;

    public event Action? OnGameEnded; // 게임 종료 시에 동작 - 성공, 실패 공용

    private void Awake()
    {
        // 싱글톤 보장
        if (Instance! == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        DOTween.SetTweensCapacity(500, 50); // 동시에 실행될 애니메이션의 수 / 여러 애니메이션이 순차적으로 실행되는 수

        Enemy.OnEnemyDespawned += HandleEnemyDespawned;
        OnGameEnded += ClearStageObjectPools;
    }

    private void Start()
    {
        // Awake에서 UIManager null 에러가 갑자기 떠서 Start로 옮겨놨음
        StageUIManager.Instance!.UpdateSpeedUpButtonVisual(StageManager.Instance!.IsSpeedUp);
        StageUIManager.Instance!.UpdatePauseButtonVisual();

        if (GameManagement.Instance != null)
        {
            // StageLoader에서 스테이지 시작을 처리함
            return;
        }
    }

    private void Update()
    {
        if (currentState == GameState.Battle)
        {
            if (Time.time - lastCostUpdateTime > COST_CHECK_INTERVAL)
            {
                Debug.LogWarning("1초 동안 코스트 업데이트가 감지되지 않음, 코스트 회복 재시작");
                RestartCostIncrease();
            }
        }
    }

    public void InitializeStage(StageData stageData, List<SquadOperatorInfo> squadData, StageLoadingScreen stageLoadingScreen)
    {
        this.stageData = stageData;
        this.stageLoadingScreen = stageLoadingScreen;

        // 맵 준비
        InitializeMap();

        // 스테이지에 필요한 모든 재료(오브젝트 풀)를 준비함
        PreloadStageObjectPools(squadData);

        // 맵에서 가져올 게 있어서 맵 초기화 후에 진행해야 함
        PrepareDeployables(squadData);

        // 스테이지 준비
        PrepareStage();

        // 로딩 화면이 사라진 후에 StartStage가 동작함
        stageLoadingScreen.OnHideComplete += StartStageCoroutine;
    }

    private void PrepareStage()
    {
        InstanceValidator.ValidateInstance(stageData);

        SetGameState(GameState.Preparation);

        // 게임 설정

        // 적의 숫자 초기화
        TotalEnemyCount = CalculateTotalEnemyCount();
        KilledEnemyCount = 0;

        // 코스트 관련 초기화
        currentDeploymentCost = stageData!.startDeploymentCost;
        maxDeploymentCost = stageData!.maxDeploymentCost;
        timeToFillCost *= stageData!.costPerSecondModifier;

        // 체력 포인트 초기화
        CurrentLifePoints = MaxLifePoints;

        // UI 초기화
        StageUIManager.Instance!.Initialize();
         
        OnPreparationCompleted?.Invoke();
    }

    private void StartStageCoroutine()
    {
        StartCoroutine(StartStageWithDelay());
    }

    private IEnumerator StartStageWithDelay()
    {
        yield return new WaitForSecondsRealtime(0.5f); // Time.timeScale에 영향을 받지 않게 구성

        if (SpawnerManager.Instance! == null) throw new InvalidOperationException("스포너 매니저 인스턴스가 없음");

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

        count += spawners! // spawners가 null이 아님을 가정
            .Sum(spawner =>
            spawner.spawnList?.spawnedEnemies?.Count(spawnInfo => spawnInfo.spawnType is SpawnType.Enemy) 
            ?? 0 // spawnList나 spawnedEnemies가 null이면 0을 사용
            );
        

        return count;
    }

    // 게임의 상태를 변경하고 그에 맞는 시간 속도 지정
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
        // 다른 필요한 상태 관련 로직...
    }

    private IEnumerator IncreaseCostOverTime()
    {
        while (true)
        {
            if (currentState == GameState.Battle)
            {
                yield return null; // 매 프레임마다 실행

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

            // Pause, Battle이 아닐 때 코루틴 종료
            if (currentState != GameState.Battle && currentState != GameState.Paused)
            {
                yield break; 
            }

            yield return null;
        }
    }

    // 배치 시 코스트 감소
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
                Debug.LogError("처리되면 안되는 듯?");
                break;
        }
    }

    // Enemy가 잡혔을 때마다 호출
    public void OnEnemyDefeated(Enemy enemy)
    {
        KilledEnemyCount++;
        StageUIManager.Instance!.UpdateEnemyKillCountText();

        // 사실 "생성된" 적을 포함하면 조건을 조금 더 다르게 줘야 함
        // 아직은 생성된 적이 없기 때문에 이렇게 구현.
        if (KilledEnemyCount + PassedEnemies >= TotalEnemyCount)
        {
            StartCoroutine(GameWinAfterDelay());
        }
    }

    private IEnumerator GameWinAfterDelay()
    {
        yield return new WaitForSeconds(0.22f); // 적이 사라지는 시간이 0.2초니까 그거보다 조금 더 길게
        GameWin();
    }

    private IEnumerator GameOverAfterDelay()
    {
        yield return new WaitForSeconds(0.22f); // 적이 사라지는 시간이 0.2초니까 그거보다 조금 더 길게
        GameOver();
    }

    public void OnEnemyReachDestination(Enemy enemy)
    {
        if (currentState == GameState.Battle)
        {
            CurrentLifePoints -= enemy.BaseData.PlayerDamage; // OnLifePointsChanged : 생명력이 깎이면 자동으로 UI 업데이트 발생
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

        // 아이템 보상 설정 및 지급
        // RewardManager.SetAndGrantStageRewards와 동일하지만, SetStageRewards에서 반환되는 값을 StageManager에서 갖고 있어야 한다는 차이점은 있다.
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

    // 유저가 스테이지에서 빠져나가는 버튼을 눌렀을 때 동작
    public void RequestExit()
    {
        // 게임이 끝난 상태의 중복 호출 방지
        if (currentState == GameState.GameWin || currentState == GameState.GameOver) return;

        // GameOver로 통합
        GameOver();

    }

    public void ReturnToMainMenu(bool isPerfectClear = false)
    {
        if (GameManagement.Instance! == null) throw new InvalidOperationException("GameManagement 초기화 안됨");

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
        Debug.Log("Pause 동작");

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
        lastCostUpdateTime = Time.time; // 계속적인 재실행 대비
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
                    Debug.LogError("맵 ID가 스테이지 설정과 일치하지 않습니다!");
                    return;
                }

                mapObject.name = "Map";
                mapObject.transform.SetParent(MapManager.Instance!.transform);

                // 위치 초기화
                mapObject.transform.localPosition = Vector3.zero;
                mapObject.transform.localRotation = Quaternion.identity;

                MapManager.Instance!.InitializeMap(currentMap);
                OnMapLoaded?.Invoke(currentMap);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"맵 초기화 중 오류 발생 : {e.Message}");
                return;
            }
            
        }
    }

    // 배치 가능한 유닛 리스트를 준비합니다. 오퍼레이터 + 스테이지에서 사용 가능한 오브젝트
    private void PrepareDeployables(List<SquadOperatorInfo> squadData)
    {
        InstanceValidator.ValidateInstance(StageData);

        // 맵에서 배치 가능한 요소를 가져옴. 없으면 빈 리스트
        var mapDeployables = StageData!.mapDeployables ?? new List<MapDeployableData>();

        // 복사해서 사용
        var deployableList = new List<MapDeployableData>(mapDeployables);

        // 스쿼드 + 맵의 배치 가능 요소 초기화
        DeployableManager.Instance!.Initialize(squadData, deployableList, stageData!.operatorMaxDeploymentCount);
    }


    // 스테이지 종료 시에 모든 오브젝트 풀을 파괴합니다.
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
            Debug.LogError("ObjectPoolManager가 없어 풀 생성 불가능");
            return;
        }

        // 1. 등장할 모든 적 유닛 목록 확보
        var uniqueEnemyPrefabs = new HashSet<GameObject>();

        // 프리팹 수집
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
        // 풀 생성
        foreach (var enemyPrefab in uniqueEnemyPrefabs)
        {
            Enemy enemy = enemyPrefab.GetComponent<Enemy>();
            if (enemy == null || enemy.BaseData == null) continue;
            EnemyData enemyData = enemy.BaseData;

            string enemyPoolTag = enemyData.GetUnitTag();
            ObjectPoolManager.Instance.CreatePool(enemyPoolTag, enemyPrefab, 10);
            Debug.Log($"enemy 풀 - {enemyPoolTag} 생성됨");

        }

        // 2. 스쿼드의 모든 배치 가능 요소 유닛 목록 확보 및 풀링 (프리팹 수집 + 풀 생성)
        // a. 스쿼드
        foreach (var opInfo in squadData)
        {
            if (opInfo.op.OperatorProgressData != null)
            {
                OperatorData opData = opInfo.op.OperatorProgressData;
                GameObject operatorPrefab = opData.prefab;

                string opPoolTag = opData.GetUnitTag();
                ObjectPoolManager.Instance.CreatePool(opPoolTag, operatorPrefab, 1);
                Debug.Log($"operator 풀 - {opPoolTag} 생성됨");
            }
        }
        // b. 맵 전용
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
                    Debug.Log($"operator 풀 - {deployablePoolTag} 생성됨");
                }
            }
        }

        // 3. 종속 오브젝트 풀링
        // 3-a. Enemy의 종속 오브젝트
        foreach (var enemyPrefab in uniqueEnemyPrefabs)
        {
            Enemy enemyComponent = enemyPrefab.GetComponent<Enemy>();
            if (enemyComponent == null || enemyComponent.BaseData == null) continue;

            EnemyData enemyBaseData = enemyComponent.BaseData;
            enemyBaseData.CreateObjectPools();
            Debug.Log($"{enemyBaseData} 관련 종속 오브젝트 풀 생성됨");
        }

        // 3-b. Opereator의 종속 오브젝트
        foreach (var opInfo in squadData)
        {
            OperatorData opData = opInfo.op.OperatorProgressData;
            if (opData == null) continue;
            opData.CreateObjectPools();

            // 스킬 오브젝트 풀 생성
            // "선택된 스킬"이라는 정보는 여기나 스쿼드 단위에서 관리되므로 여기서 구현
            int skillIndex = opInfo.skillIndex;
            if (skillIndex >= 0 && skillIndex < 2)
            {
                // 인덱스는 0 or 1
                OperatorSkill selectedSkill = skillIndex == 0 ? opData.elite0Skill : opData.elite1Unlocks.unlockedSkill;
                if (selectedSkill != null)
                {
                    selectedSkill.PreloadObjectPools(opData);
                }
            }

            Debug.Log($"{opData} 관련 종속 오브젝트 풀 생성됨");
        }

        // 3-c. 맵 전용 배치 유닛의 종속 오브젝트 풀링
        if (stageData != null && stageData.mapDeployables != null)
        {
            foreach (var mapDeployableData in stageData.mapDeployables)
            {
                if (mapDeployableData.DeployableData == null) continue;

                DeployableUnitData deployableData = mapDeployableData.DeployableData;
                if (deployableData != null)
                {
                    deployableData.CreateObjectPools();
                    Debug.Log($"{deployableData} 관련 종속 오브젝트 풀 생성됨");
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