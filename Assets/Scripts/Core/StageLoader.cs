using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// ����
/// 1. �������� �� ��ȯ, �ε� ���μ��� ����
/// 2. �� ������ ���� �� �⺻ ����
/// 3. �Ŵ������� �ʱ�ȭ ���� ����
/// </summary>
public class StageLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject loadingScreenPrefab;

    private StageData cachedStageData;
    private List<OperatorData> cachedSquadData;
    private bool isLoading;

    private GameObject loadingScreen;
    private const float MIN_LOADING_TIME = 0.5f;

    public event System.Action<Map> OnMapLoaded;

    public void LoadStage(StageData stageData)
    {
        if (isLoading)
        {
            Debug.LogWarning("���������� �̹� �ε� ����");
            return;
        }

        cachedStageData = stageData;
        cachedSquadData = new List<OperatorData>(GameManagement.Instance.UserSquadManager.GetCurrentSquad());

        StartCoroutine(LoadStageRoutine());
    }

    private IEnumerator LoadStageRoutine()
    {
        isLoading = true;
        float loadStartTime = Time.time;

        // �ε� ȭ�� �����ֱ�
        ShowLoadingScreen();

        // �񵿱� �� �ε�
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(cachedStageData.sceneToLoad);
        asyncLoad.allowSceneActivation = false; // 90%���� �ϴ� ����

        // �� �ε� ����
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

        // �������� �� Ȱ��ȭ
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return StartCoroutine(InitializeStageRoutine());

        isLoading = false;
        HideLoadingScreen();
    }

    /// <summary>
    /// �������� ���� �ε�� ��Ȳ�� ������
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitializeStageRoutine()
    {
        if (StageManager.Instance == null)
        {
            Debug.LogError("�������� �Ŵ����� ������ �߰ߵ��� ����");
            ReturnToMainMenu();
            yield break;
        }

        // 1. �� ����
        yield return StartCoroutine(InitializeMap());

        // 2. ��ġ ������ ���� �ʱ�ȭ
        if (!InitializeDeployables())
        {
            ReturnToMainMenu();
            yield break;
        }
        yield return null;

        // 3. �������� �Ŵ��� �ʱ�ȭ
        StageManager.Instance.PrepareStage();

        // 4. �������� �Ŵ����� �������� ����
        StageManager.Instance.StartStage();

        CleanupCache();
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
        if (DeployableManager.Instance == null)
        {
            Debug.LogError("DeployableManager�� �����ϴ�");
            return false;
        }

        if (cachedSquadData == null)
        {
            Debug.LogError("������ �����Ͱ� �����ϴ�");
            return false;
        }

        if (DeployableManager.Instance != null)
        {
            DeployableManager.Instance.Initialize();

            foreach (var opData in cachedSquadData)
            {
                if (opData != null)
                {
                    var deployableInfo = new DeployableManager.DeployableInfo
                    {
                        prefab = opData.prefab,
                        maxDeployCount = 1,
                        remainingDeployCount = 1,
                        isUserOperator = true,
                        redeployTime = opData.stats.RedeployTime
                    };

                    DeployableManager.Instance.AddDeployableInfo(opData.prefab, 1, true);
                }
            }
        }

        return true;
    }

    private void ShowLoadingScreen()
    {
        if (loadingScreenPrefab != null && loadingScreen == null)
        {
            loadingScreen = Instantiate(loadingScreenPrefab);
            DontDestroyOnLoad(loadingScreen);
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

    private void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void CleanupCache()
    {
        cachedStageData = null;
        cachedSquadData?.Clear();
        cachedSquadData = null;
    }

    public void OnSceneLoaded()
    {
        isLoading = false;
        CleanupCache();
    }

    public void OnGameQuit()
    {
        CleanupCache();
        HideLoadingScreen();
    }
}
