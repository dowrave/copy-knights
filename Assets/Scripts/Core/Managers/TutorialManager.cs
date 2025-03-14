using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    // 여러 씬에서 쓰이기 때문에 프리팹 형태로 넣음
    [Header("References")]
    [SerializeField] private ConfirmationPanel confirmPanelPrefab = default!;
    [SerializeField] private GameObject tutorialPanelPrefab = default!;
    [SerializeField] private TutorialData tutorialData;

    private int currentStepIndex = -1;
    private bool isTutorialActive = false;
    private TutorialPanel? currentTutorialPanel;
    private Button? currentOverlay; // 현재 Step의 목표 버튼 위에 나타나는 투명한 버튼

    private ConfirmationPanel confirmPanelInstance;

    private Canvas? canvas;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // 튜토리얼 시작 여부를 묻는 패널
        if (!CheckTutorialFinished())
        {
            InitializeTutorialConfirmPanel();
        }
        else
        {
            Debug.LogError("PlayerData, 혹은 TutorialData가 정상적으로 초기화되지 않았음");
        }
    }



    // 튜토리얼 진행 여부를 검사
    private bool CheckTutorialFinished()
    {
        return GameManagement.Instance!.PlayerDataManager.IsTutorialFinished();
    }

    private void InitializeTutorialConfirmPanel()
    {
        Canvas canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
        confirmPanelInstance = Instantiate(confirmPanelPrefab, canvas.transform);
        confirmPanelInstance.Initialize("최초 실행이 감지되었습니다. 튜토리얼을 진행하시겠습니까?", true, false);
        confirmPanelInstance.OnConfirm += CheckStartTutorial;
        confirmPanelInstance.OnCancel += CheckStopTutorial;
    }


    private void CheckStartTutorial()
    {
        confirmPanelInstance.OnConfirm -= CheckStartTutorial;
        confirmPanelInstance.OnCancel -= CheckStopTutorial;

        StartCoroutine(ShowTutorialStartPanel("튜토리얼을 시작합니다.", false, true));
    }

    private void CheckStopTutorial()
    {
        confirmPanelInstance.OnConfirm -= CheckStartTutorial;
        confirmPanelInstance.OnCancel -= CheckStopTutorial;
    }

    private IEnumerator ShowTutorialStartPanel(string message, bool isCancelButton, bool blurAreaActivation)
    {
        // 패널 전환을 위한 약간의 지연
        yield return new WaitForSeconds(0.1f);

        // 동일한 인스턴스로 새 메시지 표시
        confirmPanelInstance.Initialize(message, isCancelButton, blurAreaActivation);

        confirmPanelInstance.OnConfirm += StartTutorial;
    }

    private void StartTutorial()
    {
        StartCoroutine(StartTutorialWithDelay());
    }

    private IEnumerator StartTutorialWithDelay()
    {
        yield return new WaitForSeconds(0.1f);
        confirmPanelInstance.OnConfirm -= StartTutorial;

        isTutorialActive = true;
        currentStepIndex = 0;

        PlayCurrentStep();
    }

    public void StopTutorial()
    {
        isTutorialActive = false;
        currentTutorialPanel.gameObject.SetActive(false);
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
        
        // 마지막 스텝이었다면 튜토리얼 종료
        if (currentStepIndex == tutorialData.steps.Count)
        {
            StopTutorial();
            return;
        }

        // 아니라면 계속 진행
        AdvanceToNextStep();
    }

    private void PlayCurrentStep()
    {
        TutorialData.TutorialStep currentStep = tutorialData.steps[currentStepIndex];

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

    private void AdvanceToNextStep()
    {
        // 마지막 스텝이었다면 튜토리얼 종료
        if (currentStepIndex >= tutorialData.steps.Count - 1)
        {
            StopTutorial();
            return;
        }

        // 다음 스텝으로 인덱스 이동
        currentStepIndex++;

        // 스텝 실행
        PlayCurrentStep();
    }

    // 현재 스텝이 끝난 상태에서 다음 스텝으로 이동하기 전, 사용자의 동작을 기다리는 메서드
    private IEnumerator WaitForUserAction(string expectedButtonName)
    {
        bool actionReceived = false;

        // 특정 버튼을 클릭해도 되지 않는 경우
        if (expectedButtonName == string.Empty)
        {
            // tutorialPanel의 가장 위에 오는 transparentPanel에 리스너를 추가함
            currentTutorialPanel.AddClickListener(() => actionReceived = true);

            // 버튼 입력 대기
            while (!actionReceived) yield return null;
            CurrentStepFinish();
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
            yield return new WaitForSeconds(waitSeconds + 0.01f);

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
        yield return new WaitForSeconds(waitSeconds);

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


    private void InitializeOverlay()
    {

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

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        canvas = FindRootCanvas();
        if (canvas == null) Debug.LogError("TutorialManager에 canvas가 정상적으로 할당되지 않음");
    }
}
