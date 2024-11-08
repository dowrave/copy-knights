using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }

    private void ValidateComponents()
    {
        if (stageLoader == null || userSquadManager == null)
        {
            Debug.LogError("GameManagement : SerializeField�� Ȯ���غ� ��");
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
