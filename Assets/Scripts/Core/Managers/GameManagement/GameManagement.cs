using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환 과정에서 정보를 전달하는 매니저들을 관리한다
/// </summary>
public class GameManagement : MonoBehaviour
{
    public static GameManagement? Instance;

    [SerializeField] private StageLoader? stageLoader = null!;
    [SerializeField] private UserSquadManager? userSquadManager = null!;
    [SerializeField] private ResourceManager? resourceManager = null!;
    [SerializeField] private PlayerDataManager? playerDataManager = null!;
    [SerializeField] private TutorialManager? tutorialManager = null!;
    [SerializeField] private TimeManager? timeManager = null!;
    [SerializeField] private RewardManager? rewardManager = null!;
    [SerializeField] private StageDatabase? stageDatabase = null!;
    [SerializeField] private TestManager? testManager = null!; // 추가



    [Header("System Data")]
    [SerializeField] private OperatorLevelData? operatorLevelData; // 인스펙터에서 설정

    // 프로퍼티를 사용하는 시점은 null이 아님이 보장된 시점이므로
    // null에 대한 경고를 띄우지 않게 하기 위해 `!`(= null-forgiveness 연산자)을 추가한다.
    public StageLoader StageLoader => stageLoader!;
    public UserSquadManager UserSquadManager => userSquadManager!;
    public ResourceManager ResourceManager => resourceManager!;
    public PlayerDataManager PlayerDataManager => playerDataManager!;
    public TutorialManager TutorialManager => tutorialManager!;
    public TimeManager TimeManager => timeManager!;
    public RewardManager RewardManager => rewardManager!;
    public StageDatabase StageDatabase => stageDatabase!;
    public TestManager TestManager => testManager!; // 추가



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ValidateComponents();
            InitializeSystems(); // 시스템 초기화
        }
        else
        {
            Destroy(gameObject);
        }

        // 유니티에서 씬 전환시 사용하라고 하는 방식임
        SceneManager.sceneLoaded += OnSceneLoaded; 
    }

    private void InitializeSystems()
    {
        if (operatorLevelData != null)
        {
            OperatorGrowthSystem.Initalize(operatorLevelData);
        }
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
    }

    private void ValidateComponents()
    {
        if (stageLoader == null)
            throw new NullReferenceException($"{nameof(stageLoader)}가 인스펙터에서 할당되지 않았습니다.");
        if (userSquadManager == null)
            throw new NullReferenceException($"{nameof(userSquadManager)}가 인스펙터에서 할당되지 않았습니다.");
        if (resourceManager == null)
            throw new NullReferenceException($"{nameof(resourceManager)}가 인스펙터에서 할당되지 않았습니다.");
        if (playerDataManager == null)
            throw new NullReferenceException($"{nameof(playerDataManager)}가 인스펙터에서 할당되지 않았습니다.");
        if (operatorLevelData == null)
            throw new NullReferenceException($"{nameof(operatorLevelData)}가 인스펙터에서 할당되지 않았습니다.");
    }

    private void OnApplicationQuit()
    {
        if (stageLoader != null)
        {
            stageLoader.OnGameQuit();
        }
    }
}
