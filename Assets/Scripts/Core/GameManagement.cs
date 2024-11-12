using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환 과정에서 정보를 전달하는 매니저들을 관리한다
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

        // 유니티에서 씬 전환시 사용하라고 하는 방식임
        SceneManager.sceneLoaded += OnSceneLoaded; 
    }

    private void ValidateComponents()
    {
        if (stageLoader == null || userSquadManager == null)
        {
            Debug.LogError("GameManagement : SerializeField를 확인해볼 것");
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
