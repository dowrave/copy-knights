using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
        // ��� �Է� ����(�������� �Է� ��ư ��Ŭ�� ������ �ʿ�)
        EventSystem.current.enabled = false;

        // �ε� ȭ�� �����ֱ� �� �Է� ����
        ShowLoadingScreen();

        // �ε� ��ũ���� ������ �ð��� ��
        yield return null;

        // �񵿱� �� �ε�
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(STAGE_SCENE);
        asyncLoad.allowSceneActivation = false;

        // �� �ε� ���� 
        // �� �ε�(50%)�� �������� �ʱ�ȭ(50%)�� ������� ������
        while (asyncLoad.progress < 0.9f) // ��׶��� �ε� �Ϸ� ������ 0.9f
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            loadingScreen?.UpdateProgress(progress * 0.5f); // ��ü �ε��� 50%
            yield return null;
        }
        asyncLoad.allowSceneActivation = true;

        // true�� ���� ���� �ε��� Ȱ��ȭ�� ��� �Ϸ�� ����
        // �ش� ���� ��� Ȱ�� ���� ������Ʈ�� Awake �޼��尡 ȣ��ȴ�.
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // �� ��ȯ �Ϸ� �� StageManager �ʱ�ȭ�� ����ȴ�.
        // �ε�ȭ���� ����� �κ��� StageManager���� ����ų ��.
        if (CachedStageData != null && cachedSquadData != null && loadingScreen != null)
        {
            StartCoroutine(StageManager.Instance!.InitializeStageCoroutine(
                CachedStageData,
                cachedSquadData,
                loadingScreen,
                progress => loadingScreen.UpdateProgress(0.5f + progress * 0.5f)
            ));
        }

        // �ʱ�ȭ �Ŀ��� �ε�ȭ�� ����� : StageManager���� OnHideComplete���� ������
        // loadingScreen = null;
        isLoading = false;
        EventSystem.current.enabled = true; // �Է� ���� ���� - StageManager �κ��� �� �����ϴٴ� ��
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
