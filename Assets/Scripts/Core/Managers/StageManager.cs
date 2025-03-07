using UnityEngine;
using System.Collections; // IEnumerator - 코루틴에서 주로 사용하는 버전
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System;

// 스테이지 씬에서 스테이지와 관련된 여러 상태들을 관리합니다.
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    public GameState currentState;
    private StageData stageData;
    public StageData StageData => stageData;

    private Map currentMap;

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
    public bool IsSpeedUp { get; private set; } = false;

    private const float speedUpScale = 2f;
    private const float originalTimeScale = 1f;
    private const float placementTimeScale = 0.2f;

    private float gameEndDelay = 0.2f; // 게임 종료 조건 달성 후 기다리는 시간

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
    private Coroutine costIncreaseCoroutine;
    private float lastCostUpdateTime;
    private const float COST_CHECK_INTERVAL = 1f;

    // 스크린이 완전히 사라진 다음 게임 시작을 위한 할당
    StageLoadingScreen stageLoadingScreen;

    // 이벤트
    public event System.Action<Map> OnMapLoaded;
    public event System.Action OnDeploymentCostChanged; // 이벤트 발동 조건은 currentDeploymentCost 값이 변할 때, 여기 등록된 함수들이 동작
    public event System.Action<int> OnLifePointsChanged; // 라이프 포인트 변경 시 발생 이벤트
    public event System.Action OnEnemyKilled; // 적을 잡을 때마다 발생 이벤트
    public event System.Action OnPreparationCompleted; // 스테이지 준비 완료 이벤트 
    public event System.Action<GameState> OnGameStateChanged;
    public event System.Action OnGameEnded; // 게임 종료 시에 동작

    private void Awake()
    {
        // 싱글톤 보장
       if (Instance == null)
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
        UIManager.Instance.UpdateSpeedUpButtonVisual();
        UIManager.Instance.UpdatePauseButtonVisual();

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
        SetGameState(GameState.Preparation);

        // 게임 설정

        // 적의 숫자 초기화
        TotalEnemyCount = CalculateTotalEnemyCount();
        KilledEnemyCount = 0;

        // 코스트 관련 초기화
        currentDeploymentCost = stageData.startDeploymentCost;
        maxDeploymentCost = stageData.maxDeploymentCost;
        timeToFillCost = stageData.timeToFillCost;

        // 체력 포인트 초기화
        CurrentLifePoints = MaxLifePoints;

        // UI 초기화
        UIManager.Instance.Initialize();
         
        OnPreparationCompleted?.Invoke();
    }

    private void StartStageCoroutine()
    {
        StartCoroutine(StartStageWithDelay());
    }

    private IEnumerator StartStageWithDelay()
    {
        yield return new WaitForSecondsRealtime(0.5f); // Time.timeScale에 영향을 받지 않게 구성

        if (SpawnerManager.Instance == null) throw new InvalidOperationException("스포너 매니저 인스턴스가 없음");


        SetGameState(GameState.Battle);
        lastCostUpdateTime = Time.time;
        StartCoroutine(IncreaseCostOverTime());
        SpawnerManager.Instance.StartSpawning();
    }

    private int CalculateTotalEnemyCount()
    {
        int count = 0;
        //EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>(); // 수정 필요) Map 프리팹에서 스포너들 정보 가져오는 식으로
        IReadOnlyList<EnemySpawner> spawners = currentMap.EnemySpawners;
        foreach (var spawner in spawners)
        {
            // Enemy만 전체 수량에 계산
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
        UIManager.Instance.UpdateEnemyKillCountText();

        // 사실 "생성된" 적을 포함하면 조건을 조금 더 다르게 줘야 함
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
        if (GameManagement.Instance == null) throw new InvalidOperationException("GameManagement가 초기화되지 않았음");

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
        Time.timeScale = 0; // 게임 일시 정지
        UIManager.Instance.ShowGameOverUI();
        OnGameEnded?.Invoke();
        StopAllCoroutines();
    }

    // 유저가 스테이지에서 빠져나가는 버튼을 눌렀을 때 동작
    public void RequestExit()
    {
        SetGameState(GameState.GameOver);
        Time.timeScale = 0;
        StopAllCoroutines();
        StartCoroutine(UIManager.Instance.ShowResultAfterDelay(0));
    }

    public void ReturnToMainMenu(bool isPerfectClear = false)
    {
        if (GameManagement.Instance == null) throw new InvalidOperationException("GameManagement 초기화 안됨");

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
        Debug.Log("Pause 동작");

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
        lastCostUpdateTime = Time.time; // 계속적인 재실행 대비
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
                        Debug.LogError("맵 ID가 스테이지 설정과 일치하지 않습니다!");
                        return;
                    }

                    mapObject.name = "Map";
                    mapObject.transform.SetParent(mapManager.transform);

                    // 위치 초기화
                    mapObject.transform.localPosition = Vector3.zero;
                    mapObject.transform.localRotation = Quaternion.identity;

                    mapManager.InitializeMap(currentMap);
                    OnMapLoaded?.Invoke(currentMap);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"맵 초기화 중 오류 발생 : {e.Message}");
                    return;
                }
            }
        }
    }

    // 배치 가능한 유닛 리스트를 준비합니다. 오퍼레이터 + 스테이지에서 사용 가능한 오브젝트
    private void PrepareDeployables(List<OwnedOperator> squadData)
    {
        // 맵에서 배치 가능한 요소를 가져옴. 없으면 빈 리스트
        var mapDeployables = StageData.mapDeployables ?? new List<MapDeployableData>();

        // 복사해서 사용
        var deployableList = new List<MapDeployableData>(mapDeployables);

        // 스쿼드 + 맵의 배치 가능 요소 초기화
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