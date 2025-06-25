using System.Collections;
using System.Collections.Generic;
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
        float loadStartTime = Time.time;

        // 모든 입력 차단(스테이지 입력 버튼 재클릭 때문에 필요)
        EventSystem.current.enabled = false; 

        // 로딩 화면 보여주기 및 입력 차단
        ShowLoadingScreen();

        // 비동기 씬 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(STAGE_SCENE);
        asyncLoad.allowSceneActivation = false;

        // 씬 로드 진행 - 로딩이 90% 되었을 때 다음으로 넘어간다.
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // 최소 로딩 시간 보장
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

        // 로딩 완료 직전에 입력 차단 해제
        EventSystem.current.enabled = true;

        if (CachedStageData != null && cachedSquadData != null && loadingScreen != null)
        {
            StageManager.Instance!.InitializeStage(CachedStageData, cachedSquadData, loadingScreen);
        }

        // 초기화 후에는 로딩화면 사라짐
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
