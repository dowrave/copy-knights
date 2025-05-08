using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private List<SquadOperatorInfo>? cachedSquadData;
    private bool isLoading;
    private StageLoadingScreen? loadingScreen;
    private const float MIN_LOADING_TIME = 0.5f;
    private const string MAINMENU_SCENE = "MainMenuScene"; // �������� ���� StageData ���� ����
    private const string STAGE_SCENE = "StageScene";

    // ���� �߰��� �Է� ���ܿ� ����
    private GameObject? inputBlocker;

    public void LoadStage(StageData stageData)
    {
        if (isLoading)
        {
            Debug.LogWarning("���������� �̹� �ε� ����");
            return;
        }

        cachedStageData = stageData;
        cachedSquadData = new List<SquadOperatorInfo>(GameManagement.Instance!.UserSquadManager.GetCurrentSquad());

        StartCoroutine(LoadStage());
    }

    private IEnumerator LoadStage()
    {
        float loadStartTime = Time.time;

        // ��� �Է� ����(�������� �Է� ��ư ��Ŭ�� ������ �ʿ�)
        EventSystem.current.enabled = false; 

        // �ε� ȭ�� �����ֱ� �� �Է� ����
        ShowLoadingScreen();

        // �񵿱� �� �ε�
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(STAGE_SCENE);
        asyncLoad.allowSceneActivation = false;

        // �� �ε� ���� - �ε��� 90% �Ǿ��� �� �������� �Ѿ��.
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // �ּ� �ε� �ð� ����
        float elapsedTime = Time.time - loadStartTime;
        if (elapsedTime < MIN_LOADING_TIME)
        {
            yield return new WaitForSeconds(MIN_LOADING_TIME - elapsedTime);
        }

        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // �ε� �Ϸ� ������ �Է� ���� ����
        EventSystem.current.enabled = true;

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

    public void ReturnToMainMenuWithStageSelected()
    {
        if (cachedStageData != null)
        {
            string currentStageId = cachedStageData.stageId;
            PlayerPrefs.SetString("LastPlayedStage", currentStageId);
            PlayerPrefs.Save();
            SceneManager.LoadScene(MAINMENU_SCENE);
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
    }
}
