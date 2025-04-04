using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    private List<OwnedOperator>? cachedSquadData;
    private bool isLoading;

    private StageLoadingScreen? loadingScreen;
    private const float MIN_LOADING_TIME = 0.5f;

    private const string MAINMENU_SCENE = "MainMenuScene"; // 스테이지 씬은 StageData 내에 있음
    private const string STAGE_SCENE = "StageScene";

    public void LoadStage(StageData stageData)
    {
        if (isLoading)
        {
            Debug.LogWarning("스테이지가 이미 로딩 중임");
            return;
        }

        cachedStageData = stageData;
        cachedSquadData = new List<OwnedOperator>(GameManagement.Instance!.UserSquadManager.GetCurrentSquad());

        StartCoroutine(LoadStageRoutine());
    }

    private IEnumerator LoadStageRoutine()
    {

        float loadStartTime = Time.time;

        // 로딩 화면 보여주기
        ShowLoadingScreen();

        // 비동기 씬 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(STAGE_SCENE);
        asyncLoad.allowSceneActivation = false;

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


    // 2성 이하로 클리어 / 게임 패배시에 동작
    // 메인 메뉴로 돌아가서 현재 스테이지를 선택
    public void ReturnToMainMenuWithStageSelected()
    {
        if (cachedStageData != null)
        {
            string currentStageId = cachedStageData.stageId;

            // 씬 전환 + 스테이지 선택 상태 전달
            PlayerPrefs.SetString("LastPlayedStage", currentStageId);
            PlayerPrefs.Save();

            SceneManager.LoadScene(MAINMENU_SCENE);

            // 스테이지 정보를 갖고 메인메뉴로 돌아가야 하므로 캐시를 정리하지 않음

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
