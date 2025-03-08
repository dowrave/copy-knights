using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


// ����
// 1. �������� �� ��ȯ, �ε� ���μ��� ����
// 2. �� ������ ���� �� �⺻ ����
// 3. �Ŵ������� �ʱ�ȭ ���� ����
public class StageLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject loadingScreenPrefab = default!;

    private StageData? cachedStageData;
    public StageData? CachedStageData => cachedStageData; 
    private List<OwnedOperator>? cachedSquadData;
    private bool isLoading;

    private StageLoadingScreen? loadingScreen;
    private const float MIN_LOADING_TIME = 0.5f;

    private const string MAINMENU_SCENE = "MainMenuScene"; // �������� ���� StageData ���� ����
    private const string STAGE_SCENE = "StageScene";

    public void LoadStage(StageData stageData)
    {
        if (isLoading)
        {
            Debug.LogWarning("���������� �̹� �ε� ����");
            return;
        }

        cachedStageData = stageData;
        cachedSquadData = new List<OwnedOperator>(GameManagement.Instance!.UserSquadManager.GetCurrentSquad());

        StartCoroutine(LoadStageRoutine());
    }

    private IEnumerator LoadStageRoutine()
    {

        float loadStartTime = Time.time;

        // �ε� ȭ�� �����ֱ�
        ShowLoadingScreen();

        // �񵿱� �� �ε�
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(STAGE_SCENE);
        asyncLoad.allowSceneActivation = false;

        // �� �ε� ���� - �ε��� 90% ���� �� �������� �Ѿ��.
        while (asyncLoad.progress < 0.9f)
        {
            // �ε� ������� �߰��ص� ����
            yield return null; 
        }

        // �ּ� �ε� �ð� ����
        float elpasedTime = Time.time - loadStartTime;
        if (elpasedTime < MIN_LOADING_TIME)
        {
            yield return new WaitForSeconds(MIN_LOADING_TIME - elpasedTime);
        }

        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // �� �ε� �Ϸ� �� ����
        if (CachedStageData != null && cachedSquadData != null && loadingScreen != null)
        {
            StageManager.Instance!.InitializeStage(CachedStageData, cachedSquadData, loadingScreen);

        }

        // �ʱ�ȭ �Ŀ��� �ε�ȭ�� �����
        loadingScreen = null;

    }

    private void ShowLoadingScreen()
    {
        if (loadingScreenPrefab == null) return;

        GameObject loadingObj = Instantiate(loadingScreenPrefab);
        loadingScreen = loadingObj.GetComponent<StageLoadingScreen>();

        if (loadingScreen != null && CachedStageData != null)
        {
            loadingScreen.Initialize(
                    CachedStageData.stageId,
                    CachedStageData.stageName
                );
            DontDestroyOnLoad(loadingObj);
        }
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(MAINMENU_SCENE);
        CleanupCache();
    }


    // 2�� ���Ϸ� Ŭ���� / ���� �й�ÿ� ����
    // ���� �޴��� ���ư��� ���� ���������� ����
    public void ReturnToMainMenuWithStageSelected()
    {
        if (cachedStageData != null)
        {
            string currentStageId = cachedStageData.stageId;

            // �� ��ȯ + �������� ���� ���� ����
            PlayerPrefs.SetString("LastPlayedStage", currentStageId);
            PlayerPrefs.Save();

            SceneManager.LoadScene(MAINMENU_SCENE);

            // �������� ������ ���� ���θ޴��� ���ư��� �ϹǷ� ĳ�ø� �������� ����

        }
    }
    
  
    private void CleanupCache()
    {
        cachedStageData = null;
        cachedSquadData?.Clear();
        cachedSquadData = null;
    }

    public void OnGameQuit()
    {
        CleanupCache();
        //HideLoadingScreen();
    }

    
}
