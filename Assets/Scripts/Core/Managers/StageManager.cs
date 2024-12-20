using UnityEngine;
using System.Collections; // IEnumerator - 코루틴에서 주로 사용하는 버전
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using DG.Tweening;


/*
 StageManager의 역할
1. 전체 게임 흐름 제어 
2. 게임 상태 관리
3. 적 스포너 관리
4. 플레이어 자원 관리
5. UI 업데이트 트리거
6. 승리 / 패배 조건 체크
7. 아군 캐릭터 배치 로직 관리
 */
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    public GameState currentState;
    private StageData stageData;
    public StageData StageData => stageData;

    // 배치 코스트
    // 편한 수정을 위해 엔진 상에서는 오픈
    [SerializeField] private float costIncreaseInterval = 1f; // 코스트 회복 속도의 역수
    [SerializeField] private int maxDeploymentCost = 99;
    [SerializeField] private int _currentDeploymentCost = 10;
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
            // 불필요한 업데이트 방지 - 값이 변경될 때만 코드가 실행된다
            if (_currentDeploymentCost != value)
            {
                _currentDeploymentCost = value;
                OnDeploymentCostChanged?.Invoke(); // 값이 변경될 때 이벤트 발생. 
            }
        }
    }
    public float CurrentCostGauge => currentCostGauge;
    // 이벤트
    public event System.Action OnDeploymentCostChanged; // 이벤트 발동 조건은 currentDeploymentCost 값이 변할 때, 여기 등록된 함수들이 동작
    public event System.Action<int> OnLifePointsChanged; // 라이프 포인트 변경 시 발생 이벤트
    public event System.Action OnEnemyKilled; // 적을 잡을 때마다 발생 이벤트
    public event System.Action OnPreparationComplete; // 스테이지 준비 완료 이벤트 

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

        if (GameManagement.Instance == null && 
            GameManagement.Instance.StageLoader != null)
        {
            // StageLoader에서 스테이지 시작을 처리함
            return;
        }

        // 직접 실행할 경우 
        PrepareStage();
        StartStage();
    }

    public void PrepareStage()
    {
        Debug.Log("스테이지 준비");
        SetGameState(GameState.Preparation);

        // 게임 초기화
        TotalEnemyCount = CalculateTotalEnemyCount();
        KilledEnemyCount = 0;
        CurrentLifePoints = MaxLifePoints;

        UIManager.Instance.InitializeUI();

        OnPreparationComplete?.Invoke();
    }

    public void StartStage()
    {
        Debug.Log("스테이지 시작");
        SetGameState(GameState.Battle);
        StartCoroutine(IncreaseCostOverTime());
        SpawnerManager.Instance.StartSpawning();
    }

    private int CalculateTotalEnemyCount()
    {
        int count = 0;
        EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>(); // 수정 필요) Map 프리팹에서 스포너들 정보 가져오는 식으로
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
        // 다른 필요한 상태 관련 로직...
    }

    private IEnumerator IncreaseCostOverTime()
    {
        while (currentState == GameState.Battle)
        {
            yield return null; // 매 프레임마다 실행

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

    // 적 사망 시 호출
    public void OnEnemyDefeated()
    {
        if (currentState == GameState.GameOver) return;

        KilledEnemyCount++;
        UIManager.Instance.UpdateEnemyKillCountText();

        // 사실 "생성된" 적을 포함하면 조건을 조금 더 다르게 줘야 함
        if (KilledEnemyCount + PassedEnemies >= TotalEnemyCount)
        {
            GameWin();
        }
    }

    public void OnEnemyReachDestination()
    {
        if (currentState == GameState.GameOver || currentState == GameState.GameWin) return; 

        CurrentLifePoints--; // OnLifePointsChanged : 생명력이 깎이면 자동으로 UI 업데이트 발생
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
        Time.timeScale = 0; // 게임 일시 정지
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
        Debug.Log("Pause 동작");

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