using UnityEngine;
using System.Collections; // IEnumerator - 코루틴에서 주로 사용하는 버전
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System;

// 스테이지 씬에서 스테이지와 관련된 여러 상태들을 관리합니다.
public class StageManager : MonoBehaviour
{
    public static StageManager? Instance { get; private set; }
    public GameState currentState;
    private StageData? stageData;
    public StageData? StageData => stageData;

    private Map? currentMap;

    // 배치 코스트
    private float timeToFillCost; // 코스트 1 회복에 걸리는 시간
    private int currentDeploymentCost;
    private int maxDeploymentCost;
    private float currentCostGauge = 0f;

    // 게임 상태 변수
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

    private List<ItemWithCount> actualFirstClearRewards;
    public IReadOnlyList<ItemWithCount> ActualFirstClearRewards => actualFirstClearRewards;
    private List<ItemWithCount> actualBasicClearRewards;
    public IReadOnlyList<ItemWithCount> ActualBasicClearRewards => actualBasicClearRewards;


    // 이벤트
    public event Action<Map>? OnMapLoaded;
    public event Action OnStageStarted; // GameState.Battle이 최초로 실행됐을 때
    public event Action<bool> OnSpeedUpChanged; // 배속 변화 발생
    public event Action? OnDeploymentCostChanged; // 이벤트 발동 조건은 currentDeploymentCost 값이 변할 때, 여기 등록된 함수들이 동작
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
    }

    private void Start()
    {
        // Awake에서 UIManager null 에러가 갑자기 떠서 Start로 옮겨놨음
        UIManager.Instance!.UpdateSpeedUpButtonVisual(StageManager.Instance!.IsSpeedUp);
        UIManager.Instance!.UpdatePauseButtonVisual();

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

    public void InitializeStage(StageData stageData, List<OwnedOperator> squadData, StageLoadingScreen stageLoadingScreen)
    {
        this.stageData = stageData;
        this.stageLoadingScreen = stageLoadingScreen;

        // 맵 준비
        InitializeMap();
        
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
        timeToFillCost = stageData!.timeToFillCost;

        // 체력 포인트 초기화
        CurrentLifePoints = MaxLifePoints;

        // UI 초기화
        UIManager.Instance!.Initialize();
         
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
        //InstanceValidator.ValidateInstance(spawners);

        foreach (var spawner in spawners!)
        {
            // Enemy만 전체 수량에 계산
            count += spawner.spawnList
                .Count(spawnInfo => spawnInfo.spawnType is SpawnType.Enemy);
        }
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

        UIManager.Instance!.UpdatePauseButtonVisual();
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

    // Enemy가 잡혔을 때마다 호출
    public void OnEnemyDefeated()
    {
        KilledEnemyCount++;
        UIManager.Instance!.UpdateEnemyKillCountText();

        // 사실 "생성된" 적을 포함하면 조건을 조금 더 다르게 줘야 함
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
            CurrentLifePoints--; // OnLifePointsChanged : 생명력이 깎이면 자동으로 UI 업데이트 발생
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

    // 유저가 스테이지에서 빠져나가는 버튼을 눌렀을 때 동작
    public void RequestExit()
    {
        SetGameState(GameState.GameOver);
        StopAllCoroutines();
        StartCoroutine(UIManager.Instance!.ShowResultAfterDelay(0));
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
    private void PrepareDeployables(List<OwnedOperator> squadData)
    {
        InstanceValidator.ValidateInstance(StageData);

        // 맵에서 배치 가능한 요소를 가져옴. 없으면 빈 리스트
        var mapDeployables = StageData!.mapDeployables ?? new List<MapDeployableData>();

        // 복사해서 사용
        var deployableList = new List<MapDeployableData>(mapDeployables);

        // 스쿼드 + 맵의 배치 가능 요소 초기화
        DeployableManager.Instance!.Initialize(squadData, deployableList, stageData!.operatorMaxDeploymentCount);
    }

    // 클리어 별 갯수에 따른 보상을 설정합니다. 
    public void SetStageRewards(StageData stageData, int stars)
    {
        var stageResultInfo = GameManagement.Instance!.PlayerDataManager.GetStageResultInfo(stageData.stageId);
        List<ItemWithCount> perfectFirstClearRewards = stageData.FirstClearRewardItems;
        List<ItemWithCount> perfectBasicClearRewards = stageData.BasicClearRewardItems;

        // 최초 클리어 조건. 세부 조건은 메서드 내부에 있음
        float firstClearExpItemRate = SetFirstClearExpItemRate(stageResultInfo, stars);
        float firstClearPromoItemRate = SetFirstClearPromotionItemRate(stageResultInfo, stars);
        actualFirstClearRewards = MultiplyRewards(perfectFirstClearRewards, firstClearExpItemRate, firstClearPromoItemRate);


        float basicClearExpItemRate = SetBasicClearItemRate(stars);
        actualBasicClearRewards = MultiplyRewards(perfectBasicClearRewards, basicClearExpItemRate);
    }

    // 각 reward의 count에 itemRate를 곱하여 새 리스트로 반환합니다.
    private List<ItemWithCount> MultiplyRewards(List<ItemWithCount> rewards, float expItemRate, float promoItemRate = 0f)
    {
        List<ItemWithCount> scaledRewards = new List<ItemWithCount>();

        // 3성 클리어를 반복했을 경우 배율이 0일 수 있으며, 이 경우는 빈 리스트를 반환함 
        if (expItemRate == 0f && promoItemRate == 0f) return scaledRewards; 

        foreach (var reward in rewards)
        {
            // 정예화 아이템 처리
            if (reward.itemData.type == ItemData.ItemType.EliteItem)
            {
                int scaledCount = Mathf.FloorToInt(reward.count * promoItemRate);
                scaledRewards.Add(new ItemWithCount(reward.itemData, scaledCount));
            }

            // 경험치 아이템 처리
            else
            {
                int scaledCount = Mathf.FloorToInt(reward.count * expItemRate);
                // ItemWithCount 객체를 새로 생성합니다.
                scaledRewards.Add(new ItemWithCount(reward.itemData, scaledCount));
            }
        }
        return scaledRewards;
    }

    // n성을 최초로 달성했을 때의 경험치 아이템 지급 배율을 계산한다.
    private float SetFirstClearExpItemRate(StageResultData.StageResultInfo? resultInfo, int stars)
    {
        // 클리어한 적 없을 때의 최초 보상 배율
        if (resultInfo == null)
        {
            if (stars == 1) return 0.25f;
            else if (stars == 2) return 0.5f;
            else if (stars == 3) return 1f;
        }

        // 기존에 3성으로 클리어했다면 최초 보상은 없음
        if (resultInfo.stars == 3) return 0f;

        // 기존보다 더 잘 클리어했을 때 - 남은 2, 3성의 보상을 가져감
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

        // return 문으로 빠져나가지 못했다면 에러 발생
        throw new InvalidOperationException("FirstClearItemRate의 예상치 못한 동작");
    }

    private float SetFirstClearPromotionItemRate(StageResultData.StageResultInfo? resultInfo, int stars)
    {
        // 기존에 클리어한 적이 없고 3성 클리어할 경우에만 1f 반환하여 정예화 아이템 지급
        if (resultInfo == null && stars == 3)
        {
            return 1f;
        }

        // 나머지 경우는 0 반환
        return 0f;
    }

    // n성으로 클리어했을 때의 아이템 지급 비율을 계산한다
    private float SetBasicClearItemRate(int stars)
    {
        if (stars == 1) return 0.25f;
        else if (stars == 2) return 0.5f;
        else if (stars == 3) return 1f;
        else throw new InvalidOperationException("BasicClearItemRate의 예상치 못한 동작");
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