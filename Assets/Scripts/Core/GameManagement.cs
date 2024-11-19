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

    public StageLoader StageLoader => stageLoader;
    public UserSquadManager UserSquadManager => userSquadManager;
    public ResourceManager ResourceManager => resourceManager;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ValidateComponents();
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

    public void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
    }

    private void OnApplicationQuit()
    {
        stageLoader.OnGameQuit();
    }
}
