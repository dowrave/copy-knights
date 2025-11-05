using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;


// 역할
// 1. 스테이지 씬 전환, 로딩 프로세스 관리
// 2. 맵 프리팹 생성 및 기본 설정
// 3. 매니저들의 초기화 순서 조정
public class SceneLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject loadingScreenPrefab = default!;

    private StageData? cachedStageData;
    public StageData? CachedStageData => cachedStageData;
    private List<SquadOperatorInfo>? cachedSquadData;
    private bool isStageLoading; 
    private LoadingScreen? loadingScreen;
    private bool isFadeInCompleted = false;

    private const float MIN_LOADING_TIME = 0.5f;
    private const string MAINMENU_SCENE = "MainMenuScene"; // 스테이지 씬은 StageData 내에 있음
    private const string STAGE_SCENE = "StageScene";

    // 새로 추가한 입력 차단용 변수
    private GameObject? inputBlocker;

    public void LoadStage(StageData stageData)
    {
        // 상태 초기화
        if (isFadeInCompleted) isFadeInCompleted = false;

        cachedStageData = stageData;
        cachedSquadData = new List<SquadOperatorInfo>(GameManagement.Instance!.UserSquadManager.GetCurrentSquad());

        isStageLoading = true;

        StartCoroutine(LoadScene(STAGE_SCENE));
    }

    public IEnumerator LoadScene(string sceneName)
    {
        Time.timeScale = 1f;

        // 모든 입력 차단(스테이지 입력 버튼 재클릭 때문에 필요)
        EventSystem.current.enabled = false;

        // 로딩 화면 보여주기 및 입력 차단
        ShowLoadingScreen(sceneName);

        // 페이드 인이 완료되기까지 대기 - 씬이 너무 빨리 넘어가버리는 현상이 있어서 구현
        if (loadingScreen != null) loadingScreen.OnFadeInCompleted += HandleFadeInComplete;
        yield return new WaitUntil(() => isFadeInCompleted);
        if (loadingScreen != null) loadingScreen.OnFadeInCompleted -= HandleFadeInComplete;

        // 비동기로 씬 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // 씬 로드 진행 
        // 씬 로딩(50%)과 스테이지 초기화(50%)로 진행률을 나눴음
        while (asyncLoad.progress < 0.9f) // 백그라운드 로딩 완료 시점이 0.9f
        {
            if (isStageLoading)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                loadingScreen?.UpdateProgress(progress * 0.5f); // 전체 로딩의 50%
            }
            yield return null;
        }

        asyncLoad.allowSceneActivation = true;

        // true가 된다 = 모든 활성 오브젝트의 Awake, OnEnable이 호출될 때까지 대기
        // asyncLoad.isDone이 완료되기 "직전에" SceneManager.SceneLoaded 이벤트가 호출된다.
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 스테이지 씬 진입 처리는 StageManager에서 진행
        // 이제 asyncLoad.progress = 1이며, Awake 이후이므로 StageManager에도 접근 가능하다.
        if (isStageLoading)
        {
            if (cachedStageData != null && cachedSquadData != null && loadingScreen != null)
            {
                StartCoroutine(StageManager.Instance!.InitializeStageCoroutine(
                    cachedStageData,
                    cachedSquadData,
                    loadingScreen,
                    progress => loadingScreen.UpdateProgress(0.5f + progress * 0.5f) // 후반부 로딩은 나머지 50%
                ));

                // 로딩 완료 후 대기 시간 부여
                yield return new WaitForSeconds(1f);

                // 초기화 후에는 로딩화면 사라짐 : StageManager에서 OnHideComplete에서 관리됨
            }
        }
        else
        {
            // 스테이지 로딩 상황이 아니라면 여기서 로딩스크린 파괴 처리
            CleanupCache();
            Destroy(loadingScreen.gameObject);
            loadingScreen = null;
        }
        
        EventSystem.current.enabled = true;
        isFadeInCompleted = false;
        isStageLoading = false;
    }

    private void ShowLoadingScreen(string sceneName)
    {
        if (loadingScreenPrefab == null) return;

        GameObject loadingObj = Instantiate(loadingScreenPrefab);
        loadingScreen = loadingObj.GetComponent<LoadingScreen>();

        if (loadingScreen != null)
        {
            if (sceneName == STAGE_SCENE && cachedStageData != null)
            {
                loadingScreen.Initialize(cachedStageData.stageId, cachedStageData.stageName);
            }
            else
            {
                loadingScreen.Initialize();
            }
            DontDestroyOnLoad(loadingObj);
        }
    }


    public void ReturnToMainMenu()
    {
        // SceneManager.LoadScene(MAINMENU_SCENE);
        StartCoroutine(LoadScene(MAINMENU_SCENE));
    }

    public void ReturnToMainMenuWithStageSelected()
    {
        if (cachedStageData != null)
        {
            string currentStageId = cachedStageData.stageId;
            PlayerPrefs.SetString("LastPlayedStage", currentStageId);
            PlayerPrefs.Save();
            // SceneManager.LoadScene(MAINMENU_SCENE);
            StartCoroutine(LoadScene(MAINMENU_SCENE));

        }
    }

    public void HandleFadeInComplete()
    {
        isFadeInCompleted = true;
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
