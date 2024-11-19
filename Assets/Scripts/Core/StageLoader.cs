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
    public StageData CachedStageData => cachedStageData; 
    private List<OperatorData> cachedSquadData;
    private bool isLoading;

    private StageLoadingScreen loadingScreen;
    private const float MIN_LOADING_TIME = 0.5f;

    private const string MAINMENU_SCENE = "MainMenuScene"; // 스테이지 씬은 StageData 내에 있음
    private const string STAGE_SCENE = "StageScene";

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

        // 씬 로드 진행 - 로딩이 90% 됐을 때 다음으로 넘어간다.
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

        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 씬 로딩 완료 후 진행
        yield return StartCoroutine(InitializeStageRoutine());

        isLoading = false;
        HideLoadingScreen();
    }

    /// <summary>
    /// 씬이 로드된 후에 호출됨
    /// </summary>
    private IEnumerator InitializeStageRoutine()
    {
        if (StageManager.Instance == null)
        {
            Debug.LogError("스테이지 매니저가 씬에서 발견되지 않음");
            yield break;
        }

        // 1. StageManager에 stageData 전달
        StageManager.Instance.SetStageData(CachedStageData);

        // 2. 맵 생성
        yield return StartCoroutine(InitializeMap());

        // 3. 배치 가능한 유닛 초기화
        if (!InitializeDeployables())
        {
            Debug.Log("Deployable 유닛이 초기화되지 않음");
            yield break;
        }
        yield return null;

        // 대기 화면이 있는 동안에 스테이지 준비까지 마침. 게임 시작은 대기 화면이 완전히 사라진 후
        bool loadingScreenHidden = false; 
        if (loadingScreen != null)
        {
            loadingScreen.OnHideComplete += () => loadingScreenHidden = true;

            // 준비 완료 시 이벤트 발생, StageLoadingScreen의 페이드 아웃 시작
            StageManager.Instance.PrepareStage(); 

            // WaitUntil : 괄호 내의 델리게이트(함수)가 true가 될 때까지 함수를 일시정지 시킨다. 매 프레임 검사함.
            yield return new WaitUntil(() => loadingScreenHidden);
        }


        // 5. 스테이지 매니저로 스테이지 시작
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
    /// 2성 이하로 클리어 / 게임 패배시에 동작
    /// 메인 메뉴로 돌아가서 현재 스테이지를 선택
    /// </summary>
    public void ReturnToMainMenuWithStageSelected()
    {
        string currentStageId = cachedStageData.stageId;

        // 씬 전환 + 스테이지 선택 상태 전달
        PlayerPrefs.SetString("LastPlayedStage", currentStageId);
        PlayerPrefs.Save();

        SceneManager.LoadScene(MAINMENU_SCENE);

        // 스테이지 정보를 갖고 메인메뉴로 돌아가야 하므로 캐시를 정리하지 않음
    }
    
  
    private void CleanupCache()
    {
        cachedStageData = null;
        cachedSquadData?.Clear();
        cachedSquadData = null;
    }

    /// <summary>
    /// 씬 전환 시마다 실행
    /// </summary>
    /// 

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
