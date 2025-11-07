using UnityEngine;

/// <summary>
/// 테스트용 데이터 초기화와 디버깅 기능을 관리하는 매니저
/// GameManagement의 자식 오브젝트로 구성됨
/// </summary>
public class TestManager : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool enableTestInitialization = true; // 테스트 초기화 활성화 여부
    [SerializeField] private bool enableLevelUp = false;
    [SerializeField] private bool enableInitializeSquad = false;
    [SerializeField] private bool enableTutorialTest = false;
    [SerializeField] private bool enableStageClearing = true;

    private PlayerDataManager playerDataManager;
    private RewardManager rewardManager;
    private StageDatabase stageDatabase;
    private UserSquadManager userSquadManager;

    private void Start()
    {
        // GameManagement의 자식으로 구성되므로 Instance를 통해 매니저들에 접근
        if (GameManagement.Instance == null)
        {
            Logger.LogError("GameManagement Instance가 초기화되지 않았습니다.");
            return;
        }

        InitializeReferences();
    }

    private void InitializeReferences()
    {
        playerDataManager = GameManagement.Instance.PlayerDataManager;
        rewardManager = GameManagement.Instance.RewardManager;
        stageDatabase = GameManagement.Instance.StageDatabase;
        userSquadManager = GameManagement.Instance.UserSquadManager;
    }

    public void InitializeForTest()
    {
        if (!enableTestInitialization)
        {
            Logger.Log("테스트 초기화가 비활성화되어 있습니다.");
            return;
        }

        // 튜토리얼 테스트
        if (enableTutorialTest)
        {
            TestAboutTutorial();
        }

        // 오퍼레이터 레벨업 및 정예화 테스트
        if (enableLevelUp)
        {
            InitializeOperatorsForTest();
        }

        // 스쿼드 배치
        if (enableInitializeSquad)
        {
            InitializeSquadForTest();
        }
 
        // 스테이지 클리어
        if (enableStageClearing)
        {
            InitializeStageProgressForTest();
        }

        playerDataManager.SavePlayerData();
    }


    private void TestAboutTutorial()
    {
        playerDataManager.FinishAllTutorials(); // 모든 튜토리얼 완료
        
        // 첫 번째와 두 번째 튜토리얼 완료, 세 번째는 NotStarted
        // playerDataManager.SetTutorialStatus(0, PlayerDataManager.TutorialStatus.Completed);
        // playerDataManager.SetTutorialStatus(1, PlayerDataManager.TutorialStatus.Completed);
        // playerDataManager.SetTutorialStatus(2, PlayerDataManager.TutorialStatus.NotStarted);
    }


    private void InitializeOperatorsForTest()
    {
        var ownedOperators = playerDataManager.OwnedOperators;
        foreach (var op in ownedOperators)
        {
            // 1정예화 1레벨
            op.SetPromotionAndLevel(1, 1);
        }
    }

    private void InitializeSquadForTest()
    {
        var ownedOperators = playerDataManager.OwnedOperators;
        int maxSquadSize = playerDataManager.GetMaxSquadSize();

        for (int i = 0; i < maxSquadSize && i < ownedOperators.Count; i++)
        {
            userSquadManager.TryReplaceOperator(i, ownedOperators[i], 1);
        }
    }


    private void InitializeStageProgressForTest()
    {
        // StageClearAndGetRewards("1-0", 3);
        // StageClearAndGetRewards("1-1", 3);
        // StageClearAndGetRewards("1-2", 3);
    }

    // 스테이지 클리어 및 보상 지급
    private void StageClearAndGetRewards(string stageId, int stars)
    {
        StageData stageData = stageDatabase.GetDataById(stageId);
        if (stageData != null)
        {
            rewardManager.SetAndGiveStageRewards(stageData, stars);
            playerDataManager.RecordStageResult(stageId, stars);
        }
        else
        {
            Logger.LogError($"스테이지 데이터를 찾을 수 없습니다: {stageId}");
        }
    }

    // 런타임에서 테스트 초기화 실행 (인스펙터 버튼용)
    [ContextMenu("Initialize Test Data")]
    public void ExecuteTestInitialization()
    {
        InitializeForTest();
    }
}