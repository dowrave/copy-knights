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
    [SerializeField] private GameObject loadingScreenPrefab;

    private StageData cachedStageData;
    public StageData CachedStageData => cachedStageData; 
    private List<OwnedOperator> cachedSquadData;
    private bool isLoading;

    private StageLoadingScreen loadingScreen;
    private const float MIN_LOADING_TIME = 0.5f;

    private const string MAINMENU_SCENE = "MainMenuScene"; // �������� ���� StageData ���� ����
    private const string STAGE_SCENE = "StageScene";

    public event System.Action<Map> OnMapLoaded;

    public void LoadStage(StageData stageData)
    {
        if (isLoading)
        {
            Debug.LogWarning("���������� �̹� �ε� ����");
            return;
        }

        cachedStageData = stageData;
        cachedSquadData = new List<OwnedOperator>(GameManagement.Instance.UserSquadManager.GetCurrentSquad());

        StartCoroutine(LoadStageRoutine());
    }

    private IEnumerator LoadStageRoutine()
    {
        isLoading = true;
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
        yield return StartCoroutine(InitializeStageRoutine());

        isLoading = false;
        HideLoadingScreen();
    }

    /// <summary>
    /// ���� �ε�� �Ŀ� ȣ���
    /// </summary>
    private IEnumerator InitializeStageRoutine()
    {
        if (StageManager.Instance == null)
        {
            Debug.LogError("�������� �Ŵ����� ������ �߰ߵ��� ����");
            yield break;
        }

        // 1. StageManager�� stageData ����
        StageManager.Instance.SetStageData(CachedStageData);

        // 2. �� ����
        yield return StartCoroutine(InitializeMap());

        // 3. ��ġ ������ ��ҵ� �ʱ�ȭ
        InitializeDeployables();

        yield return null;

        // ��� ȭ���� �ִ� ���ȿ� �������� �غ���� ��ħ. ���� ������ ��� ȭ���� ������ ����� ��
        bool loadingScreenHidden = false; 
        if (loadingScreen != null)
        {
            loadingScreen.OnHideComplete += () => loadingScreenHidden = true;

            // �غ� �Ϸ� �� �̺�Ʈ �߻�, StageLoadingScreen�� ���̵� �ƿ� ����
            StageManager.Instance.PrepareStage(); 

            // WaitUntil : ��ȣ ���� ��������Ʈ(�Լ�)�� true�� �� ������ �Լ��� �Ͻ����� ��Ų��. �� ������ �˻���.
            yield return new WaitUntil(() => loadingScreenHidden);
        }


        // 5. �������� �Ŵ����� �������� ����
        StageManager.Instance.StartStage();
    }

    private IEnumerator InitializeMap()
    {
        if (cachedStageData.mapPrefab != null)
        {
            MapManager mapManager = MapManager.Instance;
            if (mapManager != null)
            {
                try
                {
                    GameObject mapObject = Instantiate(cachedStageData.mapPrefab);
                    Map map = mapObject.GetComponent<Map>();

                    // MapId�� �������� �����Ϳ� �ִ� ��Id�� ��ġ�ؾ� ��
                    if (map == null || map.Mapid != cachedStageData.mapId)
                    {
                        Debug.LogError("�� ID�� �������� ������ ��ġ���� �ʽ��ϴ�!");
                        yield break;
                    }

                    mapObject.name = "Map";
                    mapObject.transform.SetParent(mapManager.transform);

                    // ��ġ �ʱ�ȭ
                    mapObject.transform.localPosition = Vector3.zero;
                    mapObject.transform.localRotation = Quaternion.identity;

                    mapManager.InitializeMap(map);
                    OnMapLoaded?.Invoke(map);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"�� �ʱ�ȭ �� ���� �߻� : {e.Message}");
                    yield break;
                }
            }
        }
    }

    private bool InitializeDeployables()
    {
        if (DeployableManager.Instance == null || cachedSquadData == null)
        {
            Debug.LogError("DeployableManager�� ������ �����Ͱ� �����ϴ�");
            return false;
        }

        // �ʿ��� ��ġ ������ ��Ҹ� ������
        var mapDeployables = new List<MapDeployableData>(MapManager.Instance.CurrentMap?.GetMapDeployables());
        if (mapDeployables == null) 
        {
            Debug.LogWarning("����Ʈ�� ���������� �ʱ�ȭ���� �ʾҽ��ϴ�"); 
            return false;
        }

        // �� ����Ʈ�� ��쵵 ����� ������ �� ����
        DeployableManager.Instance.Initialize(cachedSquadData, mapDeployables);

        return true;
    }

    private void ShowLoadingScreen()
    {
        if (loadingScreenPrefab == null) return;

        GameObject loadingObj = Instantiate(loadingScreenPrefab);
        loadingScreen = loadingObj.GetComponent<StageLoadingScreen>();

        if (loadingScreen != null)
        {
            loadingScreen.StartLoading(
                    CachedStageData.stageId,
                    CachedStageData.stageName
                );
            DontDestroyOnLoad(loadingObj);
        }
    }

    private void HideLoadingScreen()
    {
        if (loadingScreen != null)
        {
            Destroy(loadingScreen);
            loadingScreen = null;
        }
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(MAINMENU_SCENE);
        CleanupCache();
    }

    /// <summary>
    /// 2�� ���Ϸ� Ŭ���� / ���� �й�ÿ� ����
    /// ���� �޴��� ���ư��� ���� ���������� ����
    /// </summary>
    public void ReturnToMainMenuWithStageSelected()
    {
        string currentStageId = cachedStageData.stageId;

        // �� ��ȯ + �������� ���� ���� ����
        PlayerPrefs.SetString("LastPlayedStage", currentStageId);
        PlayerPrefs.Save();

        SceneManager.LoadScene(MAINMENU_SCENE);

        // �������� ������ ���� ���θ޴��� ���ư��� �ϹǷ� ĳ�ø� �������� ����
    }
    
  
    private void CleanupCache()
    {
        cachedStageData = null;
        cachedSquadData?.Clear();
        cachedSquadData = null;
    }

    /// <summary>
    /// �� ��ȯ �ø��� ����
    /// </summary>

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isLoading = false;
    }

    public void OnGameQuit()
    {
        CleanupCache();
        HideLoadingScreen();
    }

    
}
