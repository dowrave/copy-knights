using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// 역할
/// 1. 스테이지 씬 전환, 로딩 프로세스 관리
/// 2. 맵 프리팹 생성 및 기본 설정
/// 3. 매니저들의 초기화 순서 조정
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
            Debug.LogWarning("스테이지가 이미 로딩 중임");
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

        // 로딩 화면 보여주기
        ShowLoadingScreen();

        // 비동기 씬 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(cachedStageData.sceneToLoad);
        asyncLoad.allowSceneActivation = false; // 90%에서 일단 멈춤

        // 씬 로드 진행
        while (asyncLoad.progress < 0.9f)
        {
            // 로딩 진행률을 추가해도 좋다
            yield return null; 
        }

        // 최소 로딩 시간 보장
        float elpasedTime = Time.time - loadStartTime;
        if (elpasedTime < MIN_LOADING_TIME)
        {
            yield return new WaitForSeconds(MIN_LOADING_TIME - elpasedTime);
        }

        // 스테이지 씬 활성화
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
    /// 스테이지 씬이 로드된 상황을 가정함
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitializeStageRoutine()
    {
        if (StageManager.Instance == null)
        {
            Debug.LogError("스테이지 매니저가 씬에서 발견되지 않음");
            ReturnToMainMenu();
            yield break;
        }

        // 1. 맵 생성
        yield return StartCoroutine(InitializeMap());

        // 2. 배치 가능한 유닛 초기화
        if (!InitializeDeployables())
        {
            ReturnToMainMenu();
            yield break;
        }
        yield return null;

        // 3. 스테이지 매니저 초기화
        StageManager.Instance.PrepareStage();

        // 4. 스테이지 매니저로 스테이지 시작
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

                    // MapId는 스테이지 데이터에 있는 맵Id와 일치해야 함
                    if (map == null || map.Mapid != cachedStageData.mapId)
                    {
                        Debug.LogError("맵 ID가 스테이지 설정과 일치하지 않습니다!");
                        yield break;
                    }

                    mapObject.name = "Map";
                    mapObject.transform.SetParent(mapManager.transform);

                    // 위치 초기화
                    mapObject.transform.localPosition = Vector3.zero;
                    mapObject.transform.localRotation = Quaternion.identity;

                    mapManager.InitializeMap(map);
                    OnMapLoaded?.Invoke(map);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"맵 초기화 중 오류 발생 : {e.Message}");
                    yield break;
                }
            }
        }
    }

    private bool InitializeDeployables()
    {
        if (DeployableManager.Instance == null)
        {
            Debug.LogError("DeployableManager가 없습니다");
            return false;
        }

        if (cachedSquadData == null)
        {
            Debug.LogError("스쿼드 데이터가 없습니다");
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
