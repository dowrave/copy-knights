using System.Collections;
using System.Collections.Generic;
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

    public StageLoader StageLoader => stageLoader;
    public UserSquadManager UserSquadManager => userSquadManager;

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
        if (stageLoader == null || userSquadManager == null)
        {
            Debug.LogError("GameManagement : SerializeField�� Ȯ���غ� ��");
        }
    }


    public void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
    }

    private void OnApplicationQuit()
    {
        stageLoader.OnGameQuit();
        //userSquadManager.OnGameQuit();
    }
}
