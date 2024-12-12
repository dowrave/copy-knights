using System.Collections;
using System.Collections.Generic;
using System.Resources;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// �� ��ȯ �������� ������ �����ϴ� �Ŵ������� �����Ѵ�
/// </summary>
public class GameManagement : MonoBehaviour
{
    public static GameManagement Instance;

    [SerializeField] private StageLoader stageLoader;
    [SerializeField] private UserSquadManager userSquadManager;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private PlayerDataManager playerDataManager;

    [Header("System Data")]
    [SerializeField] private OperatorLevelData operatorLevelData; // �ν����Ϳ��� ����

    public StageLoader StageLoader => stageLoader;
    public UserSquadManager UserSquadManager => userSquadManager;
    public ResourceManager ResourceManager => resourceManager;
    public PlayerDataManager PlayerDataManager => playerDataManager;



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

    private void ValidateComponents()
    {
        if (stageLoader == null || userSquadManager == null || resourceManager == null)
        {
            Debug.LogError("GameManagement : SerializeField�� Ȯ���غ� ��");
        }
    }

    private void InitializeSystems()
    {
        OperatorGrowthSystem.Initalize(operatorLevelData);
    }

    public void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
    }

    private void OnApplicationQuit()
    {
        stageLoader.OnGameQuit();
    }
}
