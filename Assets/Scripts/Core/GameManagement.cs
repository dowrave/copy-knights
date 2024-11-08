using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }

    private void ValidateComponents()
    {
        if (stageLoader == null || userSquadManager == null)
        {
            Debug.LogError("GameManagement : SerializeField를 확인해볼 것");
        }
    }

    public void OnSceneLoaded()
    {
        stageLoader.OnSceneLoaded();
        //userSquadManager.OnSceneLoaded();
    }

    private void OnApplicationQuit()
    {
        stageLoader.OnGameQuit();
        //userSquadManager.OnGameQuit();
    }
}
