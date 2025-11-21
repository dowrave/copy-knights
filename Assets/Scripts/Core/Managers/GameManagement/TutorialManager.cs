using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    // 현재 튜토리얼 스텝 코루틴
    private Coroutine currentStepCoroutine;

    // 현재 강조 중인 요소
    // private GameObject highlightedOriginalObject;
    // private GameObject highlightedCopiedObject;
    // private List<GameObject> highlightedOriginalObjects;
    private List<GameObject> highlightedCopiedObjects = new List<GameObject>();
    // private Button originalActionRequiredButton;
    // private Button copiedActionRequiredButton;

    private TutorialData currentData = default!;
    private TutorialData.TutorialStep currentStep = default!;
    private int currentTutorialIndex = -1;
    private int currentStepIndex = -1;

    private TutorialCanvas? tutorialCanvas;
    // private Button? currentOverlay; // 현재 Step의 목표 버튼 위에 나타나는 투명한 버튼

    private ConfirmationPopup confirmationPopup;
    private Canvas? mainCanvas; // 스테이지의 씬마다 1개씩 있는 메인 캔버스

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
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
        // Canvas canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];

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

            currentStepCoroutine = StartCoroutine(PlayCurrentStep());
            // PlayCurrentStep();
        }

        // 상태만을 관리하고 조용히 진행시키는 요소들
        currentTutorialIndex = progress;

        // 현재 튜토리얼을 진행 중으로 저장
        GameManagement.Instance!.PlayerDataManager.SetTutorialStatus(currentTutorialIndex, PlayerDataManager.TutorialStatus.InProgress);
    }

    private IEnumerator PlayCurrentStep()
    {
        currentStep = currentData.steps[currentStepIndex];

        if (currentStep.highlightUINames.Count > 0)
        { 
            // 코루틴으로 돌려서 하이라이트를 기다리고 다음 이벤트들을 등록시키려고 함
            yield return StartCoroutine(HighlightUI(currentStep.highlightUINames, currentStep.waitTime));
            // 현재 이슈) 코루틴으로 구현하면 다음 Step으로 넘어가지지 않음

            // HighlightUI(currentStep.highlightUIName, currentStep.waitTime);
        }

        // 텍스트 출력 완료 이벤트에 사용자 동작 대기에 대한 코루틴 추가
        tutorialCanvas.OnDialogueCompleted = () =>
        {
            StartCoroutine(WaitForUserAction(currentStep.actionRequiredUIName));
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

        // 현재 스텝의 코루틴을 초기화해준 후 다음 스텝 진행
        if (currentStepCoroutine != null) StopCoroutine(currentStepCoroutine);
        currentStepCoroutine = StartCoroutine(PlayCurrentStep());
    }

    // 현재 스텝이 끝난 상태에서 다음 스텝으로 이동하기 전, 사용자의 동작을 기다리는 메서드
    private IEnumerator WaitForUserAction(string expectedButtonName)
    {
        bool actionReceived = false;

        // 클릭해야 하는 특정 버튼이 없는 경우 : 아무거나 클릭해도 다음 스텝으로 넘어가야 함
        // 하이라이트되는 요소는 있을 수도 있고 없을 수도 있음
        if (!currentStep.requireUserAction)
        {
            Logger.Log("특정 버튼을 입력할 필요가 없는 분기");

            // 하이라이트된 오브젝트가 dimPanel의 이벤트를 가로채선 안되므로 레이캐스트를 해제함
            foreach (var highlightedCopiedObject in highlightedCopiedObjects)
            {
                if (highlightedCopiedObject != null)
                {
                    CanvasGroup cg = highlightedCopiedObject.GetComponent<CanvasGroup>();
                    if (cg == null) 
                    {
                        cg = highlightedCopiedObject.AddComponent<CanvasGroup>();
                    }

                    cg.blocksRaycasts = false;
                }
            }

            // tutorialPanel의 가장 위에 오는 transparentPanel에 리스너를 추가함
            tutorialCanvas.AddClickListener(() => actionReceived = true);

            // 버튼 입력 대기
            while (!actionReceived) yield return null;

            CurrentStepFinish();
        }

        // 특정 버튼을 클릭해야만 하는 경우
        else
        {
            Logger.Log("특정 버튼의 입력이 필요한 분기");


            // OriginalButton의 OnClick 이벤트를 옮기는 과정
            Button copiedButton = FindTargetInCanvas<Button>(tutorialCanvas, currentStep.actionRequiredUIName);
            Button originalButton = FindTargetInCanvas<Button>(mainCanvas, currentStep.actionRequiredUIName); 

            if (originalButton == null || copiedButton == null)
            {
                Logger.LogError($"버튼 컴포넌트를 찾을 수 없음 : {expectedButtonName}");
                CurrentStepFinish(); // 이 과정이 막힌다고 해서 튜토리얼이 막히면 안되므로 다음으로 넘기거나 종료 처리함
                yield break;
            }

            // TutorialCanvas에 Instantiate로 새로 만든 버튼은 원본 버튼의 리스너가 등록되지 않았음
            // 그래서 원본 버튼의 리스너도 동작하게끔 구현
            copiedButton.onClick.AddListener(
                () => {
                    actionReceived = true;
                    originalButton.onClick.Invoke();
                });

            // 버튼 입력 대기
            while (!actionReceived) yield return null;

            CurrentStepFinish();
        }
    }

    // 캔버스에서의 목표 컴포넌트를 찾음
    // where은 Component를 상속받았다는 제약

    // Component로 구현한 이유는 TutorialCanvas가 Canvas를 상속받지 않아서임 - 막 갖다 붙이긴 했는데 일단 이렇게 둠
    private T FindTargetInCanvas<T>(Component canvas, string targetName) where T : Component
    {
        Logger.Log("FindTargetInCanvas 실행됨");

        // 활성화된 것만 찾고 싶으면 false, 비활성화까지 찾고 싶다면 true
        T[] components = canvas.GetComponentsInChildren<T>(false);

        foreach (T component in components)
        {
            // component.name으로 써도 동일하지만 이게 더 명확해보임
            if (component.gameObject.name == targetName)
            {
                return component;
            }
        }

        Logger.LogError($"{canvas}에서 {targetName}에 해당하는 {typeof(T).Name}을 발견하지 못함");
        return null;
    }

    private Canvas? FindMainCanvas()
    {
        // 1. MainCanvas라는 태그를 가진, 활성화된 요소를 찾음
        GameObject canvasObj = GameObject.FindGameObjectWithTag("MainCanvas"); // MainCanvas라는 태그는 인스펙터에서 직접 할당
        if (canvasObj != null) return canvasObj.GetComponent<Canvas>();

        // 2. 루트 오브젝트 중에서 MainCanvas를 가졌으면서 canvas 컴포넌트를 가진 요소를 찾음
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            // CompareTag은 비활성화된 요소도 찾을 수 있음
            if (obj.CompareTag("MainCanvas") && obj.TryGetComponent<Canvas>(out var mainCanvas))
            {
                return mainCanvas;
            }
        }

        // 못 찾으면 오류 메시지
        Logger.LogError("MainCanvas 태그를 가진 요소를 씬에서 찾지 못함");
        return null;
    }

    // 튜토리얼을 완전히 종료
    public void FinishAllTutorials()
    {
        Logger.Log("튜토리얼을 종료");
        if (currentStepCoroutine != null) StopCoroutine(currentStepCoroutine);
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
        mainCanvas = FindMainCanvas();
        if (mainCanvas == null) Logger.LogError("TutorialManager에 canvas가 정상적으로 할당되지 않음");

        // 튜토리얼용 캔버스 생성 
        if (tutorialCanvasPrefab != null)
        {
            tutorialCanvas = Instantiate(tutorialCanvasPrefab).GetComponent<TutorialCanvas>();
            Logger.Log($"{SceneManager.GetActiveScene().name}에서 tutorialCanvas 생성됨 : {tutorialCanvas.name}");
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
    public IEnumerator HighlightUI(List<string> targetNames, float waitTime)
    {
        if (tutorialCanvas == null) Logger.LogError("tutorialCanvas 인스턴스가 할당되지 않았음");

        // ResetHighlightUI();
        tutorialCanvas.gameObject.SetActive(true);

        // 1. 기다림
        yield return new WaitForSeconds(waitTime); // 애니메이션 등이 있을 수 있으니 waitTime만큼 기다린 다음 버튼을 생성함
        yield return new WaitForEndOfFrame(); // 프레임의 UI 레이아웃 계산 완료를 기다림

        // 2. 리스트의 모든 대상을 순회해서 생성함
        foreach (string targetName in targetNames)
        {
            Image targetUI = FindTargetInCanvas<Image>(mainCanvas, targetName);
            GameObject highlightedOriginalObject = targetUI.gameObject;
            // highlightedOriginalObjects.Add(highlightedOriginalObject);
                
            if (targetUI == null)
            {
                Logger.LogError($"[HighlightUI] {targetName}에 해당하는 UI를 찾지 못했음");
                yield break;
            }

            // 원본 UI를 복사해서 TutorialCanvas에 생성함
            GameObject highlightedCopiedObject = Instantiate(highlightedOriginalObject, tutorialCanvas.transform, false);
            highlightedCopiedObjects.Add(highlightedCopiedObject);
            highlightedCopiedObject.name = targetUI.name; // (Clone)이라는 오브젝트 이름 제거, 원본과 동일한 이름으로 설정

            // 텍스트 패널에는 가려져야 하므로 인덱스는 [가장 마지막 - 1]
            int childCount = tutorialCanvas.transform.childCount;
            if (childCount > 1)
            {
                highlightedCopiedObject.transform.SetSiblingIndex(childCount - 2);
            }

            // 불필요한 Layout 컴포넌트가 있다면 제거함 - SetParent 등에 의한 레이아웃 재계산을 방지하기 위해
            var layoutElement = highlightedCopiedObject.GetComponent<LayoutElement>();
            if (layoutElement != null) Destroy(layoutElement);

            // (제거할 컴포넌트가 있다면 추가로 제거)
            
            // 원본이 가진 이미지 요소를 복사된 오브젝트로 옮김
            SyncVisualHierarchy(highlightedCopiedObject, highlightedOriginalObject);

            // 복사된 오브젝트의 RectTransform을 원본에 맞춤
            EqualizeRectTransform(highlightedCopiedObject, highlightedOriginalObject);
        }
    }

    private void SyncVisualHierarchy(GameObject copy, GameObject original)
    {
        // 1. Image 컴포넌트 동기화
        // GetComponentsInChildren은 계층 구조 순서대로 반환하므로, 구조가 같다면 인덱스가 일치함
        Image[] copiedImages = copy.GetComponentsInChildren<Image>(true);
        Image[] originalImages = original.GetComponentsInChildren<Image>(true);

        if (originalImages.Length == copiedImages.Length)
        {
            for (int i = 0; i < originalImages.Length; i++)
            {
                copiedImages[i].sprite = originalImages[i].sprite;
                copiedImages[i].color = originalImages[i].color;
                copiedImages[i].fillAmount = originalImages[i].fillAmount;
                copiedImages[i].type = originalImages[i].type;
            }
        }

        // 2. TMPro 기반의 텍스트 복사
        TextMeshProUGUI[] originalTexts = original.GetComponentsInChildren<TextMeshProUGUI>(true);
        TextMeshProUGUI[] copiedTexts = copy.GetComponentsInChildren<TextMeshProUGUI>(true);

        if (originalTexts.Length == copiedTexts.Length)
        {
            for (int i = 0; i < originalTexts.Length; i++)
            {
                // 텍스트 내용 복구
                copiedTexts[i].text = originalTexts[i].text;
                copiedTexts[i].color = originalTexts[i].color;

                // 원본이 켜져 있는지 여부에 따라 똑같이 나타남
                copiedTexts[i].gameObject.SetActive(originalTexts[i].gameObject.activeSelf);
            }
        }
    }

    // RectTransform 속성을 원본과 동일하게 설정하여 위치와 크기를 맞춤
    public void EqualizeRectTransform(GameObject copiedObject, GameObject originalObject)
    {
        RectTransform originalRect = originalObject.GetComponent<RectTransform>();
        RectTransform copiedRect = copiedObject.GetComponent<RectTransform>();

        if (originalRect != null && copiedRect != null)
        {
            // 튜토리얼 캔버스의 RectTransform을 가져옴
            RectTransform tutorialCanvasRect = tutorialCanvas.GetComponent<RectTransform>();

            // [사전 설명]
            // 원본의 RectTransform 값들을 복사해서 새로운 요소에 할당해도 되지만
            // Grid Layout 등이 동작하면 제대로 동작하지 않는 이슈가 있어서
            // 최종 월드 좌표를 타겟 캔버스의 로컬 좌표 & offset으로 변환하는 방식을 사용함

            copiedRect.SetParent(tutorialCanvas.transform, false); // 월드 좌표 유지를 위해 false 처리
            copiedRect.localScale = originalRect.localScale;
            copiedRect.rotation = originalRect.rotation;

            // 1. 원본 UI의 월드 좌표 기준 네 꼭짓점 정보를 가져옴
            Vector3[] originalCorners = new Vector3[4]; // 0123 : 좌하에서 시계방향으로
            originalRect.GetWorldCorners(originalCorners);

            // 2. 복사본 UI의 앵커를 캔버스 전체로 확장
            copiedRect.anchorMin = Vector2.zero;
            copiedRect.anchorMax = Vector2.one;
            // copiedRect.pivot = new Vector2(.5f, .5f);

            // 3. 튜토리얼 캔버스의 피벗(중심)을 기준으로 한 좌표를 얻음
            // 캔버스의 중심에서 UI 요소의 좌측 하단까지 얼마나 떨어졌는가
            Vector3 btmLeft = tutorialCanvasRect.InverseTransformPoint(originalCorners[0]); 
            // 캔버스의 중심에서 UI 요소의 우측 상단까지 얼마나 떨어졌는가
            Vector3 topRight = tutorialCanvasRect.InverseTransformPoint(originalCorners[2]);

            // 4. 계산된 떨어진 거리를 여백으로 사용함
            // btmLeft, topRight는 캔버스의 중심이 기준인 값이므로
            // offsetMin은 좌측 하단(0, 0)
            // offsetMax은 우측 상단(1, 1)을 기준으로 한 좌표로 변환해서 넣어준다.
            copiedRect.offsetMin = new Vector2(btmLeft.x, btmLeft.y) - tutorialCanvasRect.rect.min;
            copiedRect.offsetMax = new Vector2(topRight.x, topRight.y) - tutorialCanvasRect.rect.max;
        }
    }

    public void ResetHighlightUI()
    {
        // 1. 리스트에 담겨있던 복제된 오브젝트들을 모두 파괴
        if (highlightedCopiedObjects != null)
        {
            foreach (GameObject obj in highlightedCopiedObjects)
            {
                if (obj != null) Destroy(obj);
            }
            
            // 2. 리스트 내용 비우기 (참조 제거)
            highlightedCopiedObjects.Clear();
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

    // 씬이 닫힐 때 호출되는 메서드
    private void OnSceneUnloaded(Scene scene)
    {
        Logger.Log("OnSceneUnloaded 동작함");
        ResetHighlightUI();

        if (tutorialCanvas != null)
        {
            Logger.Log($"{SceneManager.GetActiveScene().name}에서 tutorialCanvas 파괴됨");
            Destroy(tutorialCanvas.gameObject);
            tutorialCanvas = null;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
}
