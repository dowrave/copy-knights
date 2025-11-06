using UnityEngine;
using System.Collections; // IEnumerator - 코루틴에서 주로 사용하는 버전
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System;
using Skills.Base;
using UnityEngine.EventSystems;

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
    private LoadingScreen? stageLoadingScreen;

    // 패널에 전달하기 위한 보상 값들
    public IReadOnlyList<ItemWithCount> FirstClearRewards = default!;
    public IReadOnlyList<ItemWithCount> BasicClearRewards = default!;

    // [Header("Stage Camera")]
    // [SerializeField] private Camera stageCamera;

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

        // if (stageCamera != null) stageCamera.enabled = false; // UI 초기화 이슈로 처음에 꺼둠

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
                Logger.LogWarning("1초 동안 코스트 업데이트가 감지되지 않음, 코스트 회복 재시작");
                RestartCostIncrease();
            }
        }
    }

    // StageLoader에서의 동작이 끝난 후에 호출됨
    public IEnumerator InitializeStageCoroutine(StageData stageData, List<SquadOperatorInfo> squadData, LoadingScreen stageLoadingScreen, Action<float> onProgress)
    {
        this.stageData = stageData;
        this.stageLoadingScreen = stageLoadingScreen;

        // 맵 준비(10%)
        InitializeMap();
        onProgress?.Invoke(.1f);
        yield return null;

        // 스테이지에 필요한 모든 재료(오브젝트 풀)를 준비함
        yield return StartCoroutine(PreloadStageObjectPoolsCoroutine(squadData, progress => onProgress?.Invoke(0.1f + progress * 0.8f)));

        // 맵에서 가져올 게 있어서 맵 초기화 후에 진행해야 함
        PrepareDeployables(squadData);
        onProgress?.Invoke(0.95f);
        yield return null;

        // 스테이지 준비
        PrepareStage();
        onProgress?.Invoke(1.0f);
        yield return null;

        // 로딩 화면이 사라진 후에 StartStage가 동작함
        stageLoadingScreen.OnHideComplete += StartStage;

        yield return new WaitForSecondsRealtime(1f);
    }

    private void PrepareStage()
    {
        InstanceValidator.ValidateInstance(stageData);

        SetGameState(GameState.Preparation);

        // 적의 숫자 초기화
        TotalEnemyCount = CalculateTotalEnemyCount();
        KilledEnemyCount = 0;

        // 코스트 관련 초기화
        currentDeploymentCost = stageData!.startDeploymentCost;
        maxDeploymentCost = stageData!.maxDeploymentCost;
        timeToFillCost *= stageData!.costPerSecondModifier;

        // 체력 포인트 초기화
        CurrentLifePoints = MaxLifePoints;

        // 잠깐 대기 후 준비 완료 호출
        StartCoroutine(WaitAndIntializeUI());
    }

    private IEnumerator WaitAndIntializeUI()
    {
        // 참고) 로딩 스크린의 로딩 게이지가 1f일 때 "준비 완료" 문구가 나오는데
        // 해당 타이밍은 여기서의 대기 시간 이전임
        yield return new WaitForSecondsRealtime(.5f);

        // UI 초기화 - 스테이지의 값들이 모두 초기화된 다음에 들어가야 함
        StageUIManager.Instance!.Initialize();

        // 스테이지 준비 완료 이벤트 발생 (StageLoadingScreen의 페이드아웃 시작)
        OnPreparationCompleted?.Invoke();

    }

    // stageLoadingScreen이 완전히 사라진 후에 실행됨
    private void StartStage()
    {
        if (SpawnerManager.Instance! == null) throw new InvalidOperationException("스포너 매니저 인스턴스가 없음");

        // Debug.Log("[StageManager]StageLoadingScreen의 페이드아웃 완료 후 스테이지 시작 로직 실행");

        EventSystem.current.enabled = true; // 버튼 동작 가능하게 설정

        SetGameState(GameState.Battle);
        lastCostUpdateTime = Time.time;
        CheckTutorial();
        StartCoroutine(IncreaseCostOverTime());
        SpawnerManager.Instance!.StartSpawning();

        // stageLoadingScreen은 역할이 끝났다면 Destroy해줌 (오브젝트로 올려두지 않음!!)
        if (stageLoadingScreen != null)
        {
            stageLoadingScreen.OnHideComplete -= StartStage;
            Destroy(stageLoadingScreen.gameObject);
            stageLoadingScreen = null;
        }
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

        switch (gameState)
        {
            case GameState.Battle:
                Time.timeScale = 1f;
                break;

            case GameState.Paused:
            case GameState.GameOver:
            case GameState.GameWin:
                Time.timeScale = 0f;
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
                Logger.LogError("처리되면 안되는 듯?");
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
            GameManagement.Instance!.SceneLoader.ReturnToMainMenu();
        }
        else
        {
            GameManagement.Instance!.SceneLoader.ReturnToMainMenuWithStageSelected();
        }
    }

    public void ToggleSpeedUp()
    {
        if (currentState != GameState.Battle) return;

        IsSpeedUp = !IsSpeedUp;
        Time.timeScale = IsSpeedUp ? 2f : 1f;
        // GameManagement.Instance!.TimeManager.UpdateTimeScale(IsSpeedUp);
    }

    public void TogglePause()
    {
        Logger.Log("Pause 동작");

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
                    Logger.LogError("맵 ID가 스테이지 설정과 일치하지 않습니다!");
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
                Logger.LogError($"맵 초기화 중 오류 발생 : {e.Message}");
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

    // private void PreloadStageObjectPools(List<SquadOperatorInfo> squadData)
    private IEnumerator PreloadStageObjectPoolsCoroutine(List<SquadOperatorInfo> squadData, Action<float> onProgress)
    {
        if (ObjectPoolManager.Instance == null)
        {
            Logger.LogError("ObjectPoolManager가 없어 풀 생성 불가능");
            yield break;
        }

        // 1. 등장할 모든 적 유닛 목록 확보
        var enemyPrefabCounts = new Dictionary<GameObject, int>();

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
                        // 이미 존재하면 갯수 추가
                        if (enemyPrefabCounts.ContainsKey(spawnInfo.prefab))
                        {
                            enemyPrefabCounts[spawnInfo.prefab]++;
                        }
                        // 없다면 딕셔너리에 새로 추가
                        else
                        {
                            enemyPrefabCounts.Add(spawnInfo.prefab, 1);
                        }
                    }
                }
            }
        }

        // 풀링할 전체 작업량 계산
        float totalTasks = enemyPrefabCounts.Count + squadData.Count + (stageData?.mapDeployables?.Count ?? 0);
        float completedTasks = 0f;

        // 적 유닛 자체의 풀 & 적 유닛이 가진 오브젝트 풀 생성
        foreach (var entry in enemyPrefabCounts)
        {
            GameObject enemyPrefab = entry.Key;
            int requiredAmount = entry.Value;

            Enemy enemy = enemyPrefab.GetComponent<Enemy>();
            if (enemy == null || enemy.BaseData == null) continue;
            EnemyData enemyData = enemy.BaseData;

            string enemyPoolTag = enemyData.GetUnitTag();
            ObjectPoolManager.Instance.CreatePool(enemyPoolTag, enemyPrefab, requiredAmount);
            // Logger.Log($"enemy 풀 - {enemyPoolTag}에 오브젝트 {requiredAmount}만큼 생성됨");

            // 해당 적이 갖고 있는 오브젝트들 생성
            if (enemyData is EnemyBossData bossData) bossData.CreateObjectPools();
            else enemyData.CreateObjectPools();

            // 작업 완료 시마다 진행도 업데이트 및 제어권 양도
            completedTasks++;
            onProgress?.Invoke(completedTasks / totalTasks);
            yield return null;
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
                // Logger.Log($"operator 풀 - {opPoolTag} 생성됨");

                opData.CreateObjectPools();

                // 세부 오브젝트 풀 생성
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

                // Logger.Log($"{opData.entityName} 관련 종속 오브젝트 풀 생성 완료");

                completedTasks++;
                onProgress?.Invoke(completedTasks / totalTasks);
                yield return null;
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
                    // Logger.Log($"operator 풀 - {deployablePoolTag} 생성됨");

                    if (deployableData != null)
                    {
                        deployableData.CreateObjectPools();
                        // Logger.Log($"{deployableData} 관련 세부 오브젝트 풀 생성 완료");
                    }
                }

                completedTasks++;
                onProgress?.Invoke(completedTasks / totalTasks);
                yield return null;
            }
        }
    }

    private void OnDestroy()
    {
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