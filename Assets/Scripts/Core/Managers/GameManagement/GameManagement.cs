using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// �� ��ȯ �������� ������ �����ϴ� �Ŵ������� �����Ѵ�
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
    [SerializeField] private TestManager? testManager = null!; // �߰�



    [Header("System Data")]
    [SerializeField] private OperatorLevelData? operatorLevelData; // �ν����Ϳ��� ����

    // ������Ƽ�� ����ϴ� ������ null�� �ƴ��� ����� �����̹Ƿ�
    // null�� ���� ��� ����� �ʰ� �ϱ� ���� `!`(= null-forgiveness ������)�� �߰��Ѵ�.
    public StageLoader StageLoader => stageLoader!;
    public UserSquadManager UserSquadManager => userSquadManager!;
    public ResourceManager ResourceManager => resourceManager!;
    public PlayerDataManager PlayerDataManager => playerDataManager!;
    public TutorialManager TutorialManager => tutorialManager!;
    public TimeManager TimeManager => timeManager!;
    public RewardManager RewardManager => rewardManager!;
    public StageDatabase StageDatabase => stageDatabase!;
    public TestManager TestManager => testManager!; // �߰�



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ValidateComponents();
            InitializeSystems(); // �ý��� �ʱ�ȭ
        }
        else
        {
            Destroy(gameObject);
        }

        // ����Ƽ���� �� ��ȯ�� ����϶�� �ϴ� �����
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
            throw new NullReferenceException($"{nameof(stageLoader)}�� �ν����Ϳ��� �Ҵ���� �ʾҽ��ϴ�.");
        if (userSquadManager == null)
            throw new NullReferenceException($"{nameof(userSquadManager)}�� �ν����Ϳ��� �Ҵ���� �ʾҽ��ϴ�.");
        if (resourceManager == null)
            throw new NullReferenceException($"{nameof(resourceManager)}�� �ν����Ϳ��� �Ҵ���� �ʾҽ��ϴ�.");
        if (playerDataManager == null)
            throw new NullReferenceException($"{nameof(playerDataManager)}�� �ν����Ϳ��� �Ҵ���� �ʾҽ��ϴ�.");
        if (operatorLevelData == null)
            throw new NullReferenceException($"{nameof(operatorLevelData)}�� �ν����Ϳ��� �Ҵ���� �ʾҽ��ϴ�.");
    }

    private void OnApplicationQuit()
    {
        if (stageLoader != null)
        {
            stageLoader.OnGameQuit();
        }
    }
}
