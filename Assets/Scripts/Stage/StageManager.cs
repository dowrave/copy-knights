using UnityEngine;
//using System.Collections.Generic; // IEnumerator<T> - 제네릭 버전
using System.Collections; // IEnumerator - 코루틴에서 주로 사용하는 버전
using TMPro;
using UnityEngine.SceneManagement;

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

    // 상단 UI 요소
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI lifePointsText;

    // 게임 상태 변수
    private int totalEnemyCount;
    private int killedEnemyCount;
    private int maxLifePoints = 3;
    private int currentLifePoints;
    private int passedEnemies;

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

    // 이벤트 : System.Action은 매개변수와 반환값이 없는 메서드를 나타내는 델리게이트 타입.
    public event System.Action OnDeploymentCostChanged; // 이벤트 발동 조건은 currentDeploymentCost 값이 변할 때, 여기 등록된 함수들이 동작한다.

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
    }

    private void Start()
    {
        Debug.Log("스테이지 준비");
        InitializeStage(); // 스테이지 준비

        Debug.Log("스테이지 시작");
        StartBattle(); // 게임 시작
    }

    private void InitializeStage()
    {
        MapManager.Instance.InitializeMap();
        SetGameState(GameState.Preparation);
        StartCoroutine(IncreaseCostOverTime());

        // 게임 초기화
        totalEnemyCount = CalculateTotalEnemyCount();
        killedEnemyCount = 0;
        currentLifePoints = maxLifePoints;

        UpdateUI();
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

    private void UpdateUI()
    {
        if (enemyCountText != null)
        {
            enemyCountText.text = $"{killedEnemyCount} / {totalEnemyCount}";

        }

        if (lifePointsText != null)
        {
            lifePointsText.text = $"{currentLifePoints}";
        }
    } 

    public void StartBattle()
    {
        SetGameState(GameState.Battle);
        SpawnerManager.Instance.StartSpawning();
    }

    private void SetGameState(GameState gameState)
    {
        currentState = gameState; 

        switch (gameState)
        {
            case GameState.Battle:
                Time.timeScale = 1f;
                break;

            // switch문 한꺼번에 처리하기
            case GameState.Paused:
            case GameState.GameOver:
            case GameState.GameWin:
                Time.timeScale = 0f;
                break;
        }
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
        UpdateUI();

        // 사실 "생성된" 적을 포함하면 조건을 조금 더 다르게 줘야 함
        if (killedEnemyCount + passedEnemies >= totalEnemyCount)
        {
            GameWin();
        }
    }

    public void OnEnemyReachDestination()
    {
        if (currentState == GameState.GameOver || currentState == GameState.GameWin) return; 

        currentLifePoints--;
        passedEnemies++;
        UpdateUI();

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
}

public enum GameState
{
    Preparation,
    Battle,
    Paused,
    GameWin,
    GameOver
}