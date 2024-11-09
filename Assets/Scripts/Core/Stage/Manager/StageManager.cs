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

    // 배치 코스트
    // 편한 수정을 위해 엔진 상에서는 오픈
    [SerializeField] private float costIncreaseInterval = 1f; // 코스트 회복 속도의 역수
    [SerializeField] private int maxDeploymentCost = 99;
    [SerializeField] private int currentDeploymentCost = 10;
    private float currentCostGauge = 0f;

    // 게임 상태 변수
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
            // 불필요한 업데이트 방지 - 값이 변경될 때만 코드가 실행된다
            if (currentDeploymentCost != value)
            {
                currentDeploymentCost = value;
                OnDeploymentCostChanged?.Invoke(); // 값이 변경될 때 이벤트 발생. 
            }
        }
    }
    public float CurrentCostGauge => currentCostGauge;

    // 스테이지마다 제공되는 deployable 요소들
    [System.Serializable]
    public class StageDeployable
    {
        public GameObject deployablePrefab;
        public int maxDeployCount;
    }

    [SerializeField] private List<StageDeployable> stageDeployables = new List<StageDeployable>();

    // 이벤트
    public event System.Action OnDeploymentCostChanged; // 이벤트 발동 조건은 currentDeploymentCost 값이 변할 때, 여기 등록된 함수들이 동작
    public event System.Action<int> OnLifePointsChanged; // 라이프 포인트 변경 시 발생 이벤트
    public event System.Action OnEnemyKilled; // 적을 잡을 때마다 발생 이벤트

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
            // StageLoader에 의한 초기화 대기
            // 씬 전환에 의해 실행될 경우, Start에서 시작하면 라이프사이클이 불일치하므로 
            // StageLoader에서 스테이지 시작을 처리한다.
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
        StartCoroutine(IncreaseCostOverTime());

        // 게임 초기화
        totalEnemyCount = CalculateTotalEnemyCount();
        killedEnemyCount = 0;
        CurrentLifePoints = maxLifePoints;

        UIManager.Instance.InitializeUI();

    }

    public void StartStage()
    {
        Debug.Log("스테이지 시작");
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
        // 다른 필요한 상태 관련 로직...
    }


    public static GameObject FindStageObject()
    {
        // 모든 루트 게임 오브젝트 찾기
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        // "Stage"로 시작하는 이름을 가진 게임 오브젝트를 찾습니다.
        foreach (GameObject obj in rootObjects)
        {
            if (obj.name.StartsWith("Stage"))
            {
                return obj;
            }
        }

        return null; // Stage를 찾지 못한 경우
    }

    private IEnumerator IncreaseCostOverTime()
    {
        while (true)
        {
            yield return null; // 매 프레임마다 실행
            currentCostGauge += Time.deltaTime / costIncreaseInterval;

            if (currentCostGauge >= 1f)
            {
                currentCostGauge -= 1f;
                if (currentDeploymentCost < maxDeploymentCost)
                {
                    CurrentDeploymentCost++; // 프로퍼티의 세터도 이렇게 사용할 수 있는 듯
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

        killedEnemyCount++;
        UIManager.Instance.UpdateEnemyKillCountText();

        // 사실 "생성된" 적을 포함하면 조건을 조금 더 다르게 줘야 함
        if (killedEnemyCount + passedEnemies >= totalEnemyCount)
        {
            GameWin();
        }
    }

    public void OnEnemyReachDestination()
    {
        if (currentState == GameState.GameOver || currentState == GameState.GameWin) return; 

        
        currentLifePoints--; // OnLifePointsChanged : 생명력이 깎이면 자동으로 UI 업데이트 발생
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
        Time.timeScale = 0; // 게임 일시 정지
        UIManager.Instance.ShowGameOverUI();
        StopAllCoroutines();
    }

    private void ReturnToMenu()
    {
        Time.timeScale = 1; // 게임 속도 복구
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
}

public enum GameState
{
    Preparation,
    Battle,
    Paused,
    GameWin,
    GameOver
}