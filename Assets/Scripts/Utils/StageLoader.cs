using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;


// 역할
// 1. 스테이지 씬 전환, 로딩 프로세스 관리
// 2. 맵 프리팹 생성 및 기본 설정
// 3. 매니저들의 초기화 순서 조정
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
    private const string MAINMENU_SCENE = "MainMenuScene"; // 스테이지 씬은 StageData 내에 있음
    private const string STAGE_SCENE = "StageScene";

    // 새로 추가한 입력 차단용 변수
    private GameObject? inputBlocker;

    public void LoadStage(StageData stageData)
    {
        if (isLoading)
        {
            Debug.LogWarning("스테이지가 이미 로딩 중임");
            return;
        }

        cachedStageData = stageData;
        cachedSquadData = new List<SquadOperatorInfo>(GameManagement.Instance!.UserSquadManager.GetCurrentSquad());

        StartCoroutine(LoadStage());
    }

    private IEnumerator LoadStage()
    {
        // 모든 입력 차단(스테이지 입력 버튼 재클릭 때문에 필요)
        EventSystem.current.enabled = false;

        // 로딩 화면 보여주기 및 입력 차단
        ShowLoadingScreen();

        // 로딩 스크린이 생성될 시간을 줌
        yield return null;

        // 비동기 씬 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(STAGE_SCENE);
        asyncLoad.allowSceneActivation = false;

        // 씬 로드 진행 
        // 씬 로딩(50%)과 스테이지 초기화(50%)로 진행률을 나눴음
        while (asyncLoad.progress < 0.9f) // 백그라운드 로딩 완료 시점이 0.9f
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            loadingScreen?.UpdateProgress(progress * 0.5f); // 전체 로딩의 50%
            yield return null;
        }
        asyncLoad.allowSceneActivation = true;

        // true일 때는 씬의 로딩과 활성화가 모두 완료된 시점
        // 해당 씬의 모든 활성 게임 오브젝트의 Awake 메서드가 호출된다.
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 씬 전환 완료 후 StageManager 초기화가 진행된다.
        // 로딩화면을 숨기는 부분은 StageManager에서 담당시킬 것.
        if (CachedStageData != null && cachedSquadData != null && loadingScreen != null)
        {
            StartCoroutine(StageManager.Instance!.InitializeStageCoroutine(
                CachedStageData,
                cachedSquadData,
                loadingScreen,
                progress => loadingScreen.UpdateProgress(0.5f + progress * 0.5f)
            ));
        }

        // 초기화 후에는 로딩화면 사라짐 : StageManager에서 OnHideComplete에서 관리됨
        // loadingScreen = null;
        isLoading = false;
        EventSystem.current.enabled = true; // 입력 차단 해제 - StageManager 부분이 더 안전하다는 평
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
