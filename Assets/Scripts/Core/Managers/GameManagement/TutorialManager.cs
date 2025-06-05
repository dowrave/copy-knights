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
    [SerializeField] private GameObject tutorialPanelPrefab = default!;
    [SerializeField] private List<TutorialData> tutorialDatas = new List<TutorialData>();

    private string stageIdRequiredForTutorial = "1-0"; // 클리어가 요구되는 stage의 stageId

    private TutorialData currentData = default!;
    private TutorialData.TutorialStep currentStep = default!;
    private int currentTutorialIndex = -1;
    private int currentStepIndex = -1;

    private TutorialPanel? currentTutorialPanel;
    private Button? currentOverlay; // 현재 Step의 목표 버튼 위에 나타나는 투명한 버튼

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

        confirmationPopup = PopupManager.Instance!.ShowConfirmationPopup("최초 실행이 감지되었습니다. 튜토리얼을 진행하시겠습니까?", 
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
                GameManagement.Instance?.TimeManager.SetPauseTime();
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

        if (currentTutorialPanel == null)
        {
            Canvas canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
            currentTutorialPanel = Instantiate(tutorialPanelPrefab, canvas.transform).GetComponent<TutorialPanel>();
        }

        // 텍스트 출력 완료 이벤트에 사용자 동작 대기에 대한 코루틴 추가
        currentTutorialPanel.OnDialogueCompleted = () =>
        {
            StartCoroutine(WaitForUserAction(currentStep.expectedButtonName));
        };

        currentTutorialPanel.Initialize(currentStep);
    }

    public void CurrentStepFinish()
    {
        // 스텝마다 끝나고 진행되어야 할 요소
        if (currentOverlay != null)
        {
            // 입력 후 동작
            currentOverlay.onClick.RemoveAllListeners();

            Destroy(currentOverlay.gameObject);
            currentOverlay = null;
        }

        // 리스너 제거
        currentTutorialPanel.RemoveAllClickListeners();

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
        if (currentTutorialPanel != null)
        {
            currentTutorialPanel.gameObject.SetActive(false);   
        }

        if (currentTutorialIndex == 1)
        {
            // 시간 원상 복구
            GameManagement.Instance?.TimeManager.UpdateTimeScale();

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
                currentTutorialPanel.AddClickListener(() => actionReceived = true);

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
                Debug.LogError("요청된 버튼을 찾을 수 없습니다 : " + expectedButtonName);
                yield break;
            }

            Debug.Log("요청된 버튼을 찾았습니다 : " + expectedButton.name);

            float waitSeconds = 0.1f;

            // expectedButton과 transparent 패널 위에 오는 투명한 버튼을 만듦
            StartCoroutine(CreateCurrentOverlayAfterDelay(expectedButton, waitSeconds));

            // 위 코루틴의 실행을 기다림
            yield return new WaitForSecondsRealtime(waitSeconds + 0.01f);

            if (currentOverlay != null)
            {
                // 버튼에 리스너 추가
                currentOverlay.onClick.AddListener(() => actionReceived = true);

                // 버튼 입력 대기
                while (!actionReceived) yield return null;

                CurrentStepFinish();
            }
            else
            {
                Debug.LogError("currentOverlay에 잡힌 요소 없음");
            }
        }
    }

    // 고정된 위치의 UI가 아니라면 이런 식으로 해봄 
    private IEnumerator CreateCurrentOverlayAfterDelay(Button targetButton, float waitSeconds)
    {
        yield return new WaitForSecondsRealtime(waitSeconds);

        CreateCurrentOverlay(targetButton);
    }

    // 목표로 하는 버튼과 동일한 기능을 하는 투명한 버튼을 만듦
    // TutorialPanel의 뒷배경 패널보다 위에 요소가 오도록 하기 위함
    private void CreateCurrentOverlay(Button targetButton)
    {
        RectTransform buttonRect = targetButton.GetComponent<RectTransform>();

        // 투명한 오버레이 생성
        GameObject overlay = new GameObject("ButtonOverlay");
        overlay.transform.SetParent(canvas.transform);

        // 기존 버튼과 동일한 위치, 크기로 생성
        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = buttonRect.anchorMin;
        overlayRect.anchorMax = buttonRect.anchorMax;
        overlayRect.pivot = buttonRect.pivot;
        overlayRect.position = buttonRect.position;
        overlayRect.sizeDelta = buttonRect.sizeDelta;

        // 투명한 이미지
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0, 0, 0, 0); 

        // 클릭 이벤트 추가
        Button overlayButton = overlay.AddComponent<Button>();
        overlayButton.onClick.AddListener(() =>
        { 
            targetButton.GetComponent<Button>().onClick.Invoke();
        });

        Debug.Log("overlayButton에 리스너가 등록됨");

        // 제거를 위한 현재 오버레이 등록
        currentOverlay = overlayButton;
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
        Debug.Log("튜토리얼을 종료");
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
        if (canvas == null) Debug.LogError("TutorialManager에 canvas가 정상적으로 할당되지 않음");

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
