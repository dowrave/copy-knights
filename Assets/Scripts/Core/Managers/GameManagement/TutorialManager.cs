using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 튜토리얼 시작 조건 정리
// 1번 : 클리어 X(progress = -1) 시 확인 패널이 나타남, 확인 시 진행.
// 2번 : 1번 클리어, StageManager에서 CheckTutorial() 메서드로 여기 있는 CheckBattleStart()에서 시작
// 3번 : 스테이지 1-0 클리어, 메인 메뉴씬으로 돌아갔을 때 시작
public class TutorialManager : MonoBehaviour
{
    // 여러 씬에서 쓰이기 때문에 프리팹 형태로 넣음
    [Header("References")]
    [SerializeField] private GameObject tutorialCanvasPrefab = default!;
    [SerializeField] private List<TutorialData> tutorialDatas = new List<TutorialData>();

    private string stageIdRequiredForTutorial = "1-0"; // 클리어가 요구되는 stage의 stageId

    // 현재 강조 중인 요소
    private GameObject highlightedObject;
    private Transform originalParent;
    private int originalSiblingIndex;

    private TutorialData currentData = default!;
    private TutorialData.TutorialStep currentStep = default!;
    private int currentTutorialIndex = -1;
    private int currentStepIndex = -1;

    private TutorialCanvas? tutorialCanvas;
    // private Button? currentOverlay; // 현재 Step의 목표 버튼 위에 나타나는 투명한 버튼

    private ConfirmationPopup confirmationPopup;
    private Canvas? canvas;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // 튜토리얼 완료 시 진행하지 않음
        if (GameManagement.Instance!.PlayerDataManager.IsAllTutorialFinished()) return;

        // 튜토리얼 진행 상황 확인
        int lastCompletedTutorialIndex = GameManagement.Instance!.PlayerDataManager.GetLastCompletedTutorialIndex();

        // 튜토리얼을 시작하지 않은 상태
        if (lastCompletedTutorialIndex == -1)
        {
            InitializeTutorialConfirmPanel();
        }
    }

    private void InitializeTutorialConfirmPanel()
    {
        Canvas canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];

        confirmationPopup = PopupManager.Instance!.ShowConfirmationPopup("튜토리얼을 진행하시겠습니까?",
            isCancelButton: true,
            blurAreaActivation: true,
            onConfirm: CheckStartTutorial,
            onCancel: CheckStopTutorial);
    }

    private void CheckStartTutorial()
    {
        StartCoroutine(ShowTutorialStartPanel("튜토리얼을 시작합니다.", false, true));
    }

    private void CheckStopTutorial()
    {
        GameManagement.Instance.PlayerDataManager.FinishAllTutorials();
    }

    private IEnumerator ShowTutorialStartPanel(string message, bool isCancelButton, bool blurAreaActivation)
    {
        // 패널 전환을 위한 약간의 지연
        yield return new WaitForSecondsRealtime(0.1f);

        // 동일한 인스턴스로 새 메시지 표시
        confirmationPopup = PopupManager.Instance!.ShowConfirmationPopup(message, isCancelButton, blurAreaActivation, onConfirm: InitializeAndStartTutorial);
    }

    // 0번 튜토리얼 데이터 실행
    private void InitializeAndStartTutorial()
    {
        StartCoroutine(InitializeAndStartTutorialWithDelay());
    }

    private IEnumerator InitializeAndStartTutorialWithDelay()
    {
        yield return new WaitForSecondsRealtime(0.1f);

        // 1번째 튜토리얼을 실행시킴
        StartSpecificTutorial(0);
    }

    private void StartSpecificTutorial(int progress, bool show = true)
    {
        // 명시적으로 나타내야 하는 요소는 여기에 넣음(대화 박스 표시 등)
        if (show)
        {
            currentData = tutorialDatas[progress];
            currentStepIndex = 0;

            // 스테이지 씬의 경우 시간 멈춤
            if (progress == 1)
            {
                // GameManagement.Instance?.TimeManager.SetPauseTime();
                Time.timeScale = 0f;
            }

            PlayCurrentStep();
        }

        // 상태만을 관리하고 조용히 진행시키는 요소들
        currentTutorialIndex = progress;

        // 현재 튜토리얼을 진행 중으로 저장
        GameManagement.Instance!.PlayerDataManager.SetTutorialStatus(currentTutorialIndex, PlayerDataManager.TutorialStatus.InProgress);
    }

    private void PlayCurrentStep()
    {
        currentStep = currentData.steps[currentStepIndex];

        if (currentStep.highlightUIName != null)
        { 
            StartCoroutine(HighlightUI(currentStep.highlightUIName, currentStep.waitTime));
        }

        // 텍스트 출력 완료 이벤트에 사용자 동작 대기에 대한 코루틴 추가
        tutorialCanvas.OnDialogueCompleted = () =>
        {
            StartCoroutine(WaitForUserAction(currentStep.highlightUIName));
        };

        tutorialCanvas.Initialize(currentStep);
    }

    public void CurrentStepFinish()
    {
        ResetHighlightUI();

        // 리스너 제거
        tutorialCanvas.RemoveAllClickListeners();

        // 마지막 스텝이라면 
        if (currentStepIndex >= currentData.steps.Count - 1)
        {
            FinishCurrentTutorial(); // 이번 튜토리얼 데이터 종료
        }
        else
        {
            AdvanceToNextStep(); // 다음 스텝으로 진행
        }
    }


    // 각 TutorialData의 모든 step이 끝났을 때 호출
    private void FinishCurrentTutorial()
    {
        if (tutorialCanvas != null)
        {
            tutorialCanvas.gameObject.SetActive(false);
        }

        if (currentTutorialIndex == 1)
        {
            // 시간 원상 복구
            // GameManagement.Instance?.TimeManager.UpdateTimeScale();
            Time.timeScale = 1f;

            // 스테이지 씬에서 튜토리얼이 클리어된 시점은 게임을 클리어한 시점이라서
            // 여기서 저장을 수행하지 않음
        }
        else
        {
            GameManagement.Instance.PlayerDataManager.SetTutorialStatus(currentTutorialIndex, PlayerDataManager.TutorialStatus.Completed);

            // 마지막 튜토리얼이었다면 튜토리얼을 끝냄
            if (currentTutorialIndex == tutorialDatas.Count - 1)
            {
                FinishAllTutorials();
            }

            ResetTutorialFields();
        }
    }

    private void AdvanceToNextStep()
    {
        // 다음 스텝으로 인덱스 이동
        currentStepIndex++;

        // 스텝 실행
        PlayCurrentStep();
    }

    // 현재 스텝이 끝난 상태에서 다음 스텝으로 이동하기 전, 사용자의 동작을 기다리는 메서드
    private IEnumerator WaitForUserAction(string expectedButtonName)
    {
        bool actionReceived = false;

        // 클릭해야 하는 특정 버튼이 없는 경우 : 아무거나 클릭해도 다음 스텝으로 넘어가야 함
        if (!currentStep.requireUserAction)
        {
            if (expectedButtonName == string.Empty)
            {
                // tutorialPanel의 가장 위에 오는 transparentPanel에 리스너를 추가함
                tutorialCanvas.AddClickListener(() => actionReceived = true);

                // 버튼 입력 대기
                while (!actionReceived) yield return null;

                CurrentStepFinish();
            }
        }

        // 특정 버튼을 클릭해야만 하는 경우
        else
        {
            Button expectedButton = GameObject.Find(expectedButtonName)?.GetComponent<Button>();
            if (expectedButton == null)
            {
                Logger.LogError("요청된 버튼을 찾을 수 없습니다 : " + expectedButtonName);
                yield break;
            }

            Logger.Log("요청된 버튼을 찾았습니다 : " + expectedButton.name);

            expectedButton.onClick.AddListener(() => actionReceived = true);

            // 버튼 입력 대기
            while (!actionReceived) yield return null;

            CurrentStepFinish();
        }
    }

    private Canvas? FindRootCanvas()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

        // 루트 오브젝트 중, Canvas 컴포넌트를 가진 오브젝트 찾기
        foreach (GameObject obj in rootObjects)
        {
            Canvas? canvas = obj.GetComponent<Canvas>();
            if (canvas != null) return canvas;
        }
        return null;
    }

    // 튜토리얼을 완전히 종료
    public void FinishAllTutorials()
    {
        Logger.Log("튜토리얼을 종료");
        GameManagement.Instance.PlayerDataManager.FinishAllTutorials();
    }

    // 2번째 튜토리얼은 이걸 체크해서 시작됨
    public void StartSecondTutorial()
    {
        bool firstTutorialCleared = GameManagement.Instance!.PlayerDataManager.IsTutorialStatus(0, PlayerDataManager.TutorialStatus.Completed);

        // 1번 튜토리얼을 깼고, 2번 튜토리얼을 플레이한 적 없을 때
        if (firstTutorialCleared)
        {
            StartSpecificTutorial(1);
            StageManager.Instance!.OnGameCleared += SaveSecondTutorialClear;
            StageManager.Instance!.OnGameFailed += SaveSecondTutorialFailed;
        }
    }

    // 스테이지 Fail한 적 있을 때
    public void StartSecondTutorialQuiet()
    {
        // 튜토리얼 상태를 켜되, 조용히 실행
        StartSpecificTutorial(1, false);
        StageManager.Instance!.OnGameCleared += SaveSecondTutorialClear;
        StageManager.Instance!.OnGameFailed += SaveSecondTutorialFailed;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        canvas = FindRootCanvas();
        if (canvas == null) Logger.LogError("TutorialManager에 canvas가 정상적으로 할당되지 않음");

        // 튜토리얼용 캔버스 생성 
        if (tutorialCanvas != null) Destroy(tutorialCanvas.gameObject);

        if (tutorialCanvasPrefab != null)
        {
            tutorialCanvas = Instantiate(tutorialCanvasPrefab).GetComponent<TutorialCanvas>();
            tutorialCanvas.gameObject.SetActive(false);
        }

        // 저장된 데이터를 점검
        // - 튜토리얼이 완료/스킵된 경우 생략
        if (GameManagement.Instance.PlayerDataManager.IsAllTutorialFinished()) return;

        // 3번째 튜토리얼 실행 조건
        int lastCompletedTutorialIndex = GameManagement.Instance!.PlayerDataManager.GetLastCompletedTutorialIndex();

        if (lastCompletedTutorialIndex == 1 && // 2번째 튜토리얼이 클리어되었고
            GameManagement.Instance!.PlayerDataManager.IsTutorialStatus(2, PlayerDataManager.TutorialStatus.NotStarted)) // 3번째 튜토리얼이 실행되지 않았다면
        {
            StartSpecificTutorial(2);
        }
    }

    private void SaveSecondTutorialClear()
    {
        GameManagement.Instance.PlayerDataManager.SetTutorialStatus(currentTutorialIndex, PlayerDataManager.TutorialStatus.Completed);
        ResetTutorialFields();
    }

    private void SaveSecondTutorialFailed()
    {
        GameManagement.Instance.PlayerDataManager.SetTutorialStatus(currentTutorialIndex, PlayerDataManager.TutorialStatus.Failed);
        ResetTutorialFields();
    }

    private void ResetTutorialFields()
    {
        currentData = null;
        currentStep = null;
        currentTutorialIndex = -1;
        currentStepIndex = -1;
    }

    // 특정 UI를 강조 - 원본 캔버스에서 튜토리얼용 캔버스로 옮겨온다.
    public IEnumerator HighlightUI(string targetName, float waitTime)
    {
        if (tutorialCanvas == null) Logger.LogError("튜토리얼 인스턴스가 할당되지 않았음");

        GameObject targetUI = GameObject.Find(targetName);

        if (targetUI == null)
        {
            Logger.LogError($"{targetName}에 해당하는 UI를 찾지 못했음");
            yield return null;
        }

        tutorialCanvas.gameObject.SetActive(true);

        // 원본 UI의 정보 저장
        highlightedObject = targetUI;
        originalParent = targetUI.transform.parent;
        originalSiblingIndex = targetUI.transform.GetSiblingIndex();

        // 애니메이션 등이 있을 수 있으니 0.3초 후에 버튼을 옮김
        // 일반적인 애니메이션이 0.2초 정도임을 감안함
        yield return new WaitForSeconds(waitTime);

        // 강조할 오브젝트를 TutorialCanvas의 자식으로 만듦
        targetUI.transform.SetParent(tutorialCanvas.gameObject.transform, true);
    }

    public void ResetHighlightUI()
    {
        if (highlightedObject != null && originalParent != null)
        {
            highlightedObject.transform.SetParent(originalParent, true);
            highlightedObject.transform.SetSiblingIndex(originalSiblingIndex);

            highlightedObject = null;
            originalParent = null;
        }

        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance!.OnGameCleared -= SaveSecondTutorialClear;
            StageManager.Instance!.OnGameFailed -= SaveSecondTutorialFailed;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
