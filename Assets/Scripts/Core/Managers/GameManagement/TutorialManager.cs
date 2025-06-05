using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Ʃ�丮�� ���� ���� ����
// 1�� : Ŭ���� X(progress = -1) �� Ȯ�� �г��� ��Ÿ��, Ȯ�� �� ����.
// 2�� : 1�� Ŭ����, StageManager���� CheckTutorial() �޼���� ���� �ִ� CheckBattleStart()���� ����
// 3�� : �������� 1-0 Ŭ����, ���� �޴������� ���ư��� �� ����
public class TutorialManager : MonoBehaviour
{
    // ���� ������ ���̱� ������ ������ ���·� ����
    [Header("References")]
    [SerializeField] private GameObject tutorialPanelPrefab = default!;
    [SerializeField] private List<TutorialData> tutorialDatas = new List<TutorialData>();

    private string stageIdRequiredForTutorial = "1-0"; // Ŭ��� �䱸�Ǵ� stage�� stageId

    private TutorialData currentData = default!;
    private TutorialData.TutorialStep currentStep = default!;
    private int currentTutorialIndex = -1;
    private int currentStepIndex = -1;

    private TutorialPanel? currentTutorialPanel;
    private Button? currentOverlay; // ���� Step�� ��ǥ ��ư ���� ��Ÿ���� ������ ��ư

    private ConfirmationPopup confirmationPopup;
    private Canvas? canvas;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // Ʃ�丮�� �Ϸ� �� �������� ����
        if (GameManagement.Instance!.PlayerDataManager.IsAllTutorialFinished()) return;

        // Ʃ�丮�� ���� ��Ȳ Ȯ��
        int lastCompletedTutorialIndex = GameManagement.Instance!.PlayerDataManager.GetLastCompletedTutorialIndex();

        // Ʃ�丮���� �������� ���� ����
        if (lastCompletedTutorialIndex == -1)
        {
            InitializeTutorialConfirmPanel();
        }
    }

    private void InitializeTutorialConfirmPanel()
    {
        Canvas canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];

        confirmationPopup = PopupManager.Instance!.ShowConfirmationPopup("���� ������ �����Ǿ����ϴ�. Ʃ�丮���� �����Ͻðڽ��ϱ�?", 
            isCancelButton: true, 
            blurAreaActivation: true,
            onConfirm: CheckStartTutorial,
            onCancel: CheckStopTutorial);
    }

    private void CheckStartTutorial()
    {
        StartCoroutine(ShowTutorialStartPanel("Ʃ�丮���� �����մϴ�.", false, true));
    }

    private void CheckStopTutorial()
    {
        GameManagement.Instance.PlayerDataManager.FinishAllTutorials();
    }

    private IEnumerator ShowTutorialStartPanel(string message, bool isCancelButton, bool blurAreaActivation)
    {
        // �г� ��ȯ�� ���� �ణ�� ����
        yield return new WaitForSecondsRealtime(0.1f);

        // ������ �ν��Ͻ��� �� �޽��� ǥ��
        confirmationPopup = PopupManager.Instance!.ShowConfirmationPopup(message, isCancelButton, blurAreaActivation, onConfirm: InitializeAndStartTutorial);
    }

    // 0�� Ʃ�丮�� ������ ����
    private void InitializeAndStartTutorial()
    {
        StartCoroutine(InitializeAndStartTutorialWithDelay());
    }

    private IEnumerator InitializeAndStartTutorialWithDelay()
    {
        yield return new WaitForSecondsRealtime(0.1f);

        // 1��° Ʃ�丮���� �����Ŵ
        StartSpecificTutorial(0);
    }

    private void StartSpecificTutorial(int progress, bool show = true)
    {
        // ��������� ��Ÿ���� �ϴ� ��Ҵ� ���⿡ ����(��ȭ �ڽ� ǥ�� ��)
        if (show)
        {
            currentData = tutorialDatas[progress];
            currentStepIndex = 0;

            // �������� ���� ��� �ð� ����
            if (progress == 1)
            {
                GameManagement.Instance?.TimeManager.SetPauseTime();
            }

            PlayCurrentStep();
        }
        
        // ���¸��� �����ϰ� ������ �����Ű�� ��ҵ�
        currentTutorialIndex = progress;

        // ���� Ʃ�丮���� ���� ������ ����
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

        // �ؽ�Ʈ ��� �Ϸ� �̺�Ʈ�� ����� ���� ��⿡ ���� �ڷ�ƾ �߰�
        currentTutorialPanel.OnDialogueCompleted = () =>
        {
            StartCoroutine(WaitForUserAction(currentStep.expectedButtonName));
        };

        currentTutorialPanel.Initialize(currentStep);
    }

    public void CurrentStepFinish()
    {
        // ���ܸ��� ������ ����Ǿ�� �� ���
        if (currentOverlay != null)
        {
            // �Է� �� ����
            currentOverlay.onClick.RemoveAllListeners();

            Destroy(currentOverlay.gameObject);
            currentOverlay = null;
        }

        // ������ ����
        currentTutorialPanel.RemoveAllClickListeners();

        // ������ �����̶�� 
        if (currentStepIndex >= currentData.steps.Count - 1)
        {
            FinishCurrentTutorial(); // �̹� Ʃ�丮�� ������ ����
        }
        else
        {
            AdvanceToNextStep(); // ���� �������� ����
        }
    }


    // �� TutorialData�� ��� step�� ������ �� ȣ��
    private void FinishCurrentTutorial()
    {
        if (currentTutorialPanel != null)
        {
            currentTutorialPanel.gameObject.SetActive(false);   
        }

        if (currentTutorialIndex == 1)
        {
            // �ð� ���� ����
            GameManagement.Instance?.TimeManager.UpdateTimeScale();

            // �������� ������ Ʃ�丮���� Ŭ����� ������ ������ Ŭ������ �����̶�
            // ���⼭ ������ �������� ����
        }
        else
        {
            GameManagement.Instance.PlayerDataManager.SetTutorialStatus(currentTutorialIndex, PlayerDataManager.TutorialStatus.Completed);

            // ������ Ʃ�丮���̾��ٸ� Ʃ�丮���� ����
            if (currentTutorialIndex == tutorialDatas.Count - 1)
            {
                FinishAllTutorials();
            }

            ResetTutorialFields();
        }        
    }

    private void AdvanceToNextStep()
    {
        // ���� �������� �ε��� �̵�
        currentStepIndex++;

        // ���� ����
        PlayCurrentStep();
    }

    // ���� ������ ���� ���¿��� ���� �������� �̵��ϱ� ��, ������� ������ ��ٸ��� �޼���
    private IEnumerator WaitForUserAction(string expectedButtonName)
    {
        bool actionReceived = false;

        // Ŭ���ؾ� �ϴ� Ư�� ��ư�� ���� ��� : �ƹ��ų� Ŭ���ص� ���� �������� �Ѿ�� ��
        if (!currentStep.requireUserAction)
        {
            if (expectedButtonName == string.Empty)
            {
                // tutorialPanel�� ���� ���� ���� transparentPanel�� �����ʸ� �߰���
                currentTutorialPanel.AddClickListener(() => actionReceived = true);

                // ��ư �Է� ���
                while (!actionReceived) yield return null;


                CurrentStepFinish();
            }
        }

        // Ư�� ��ư�� Ŭ���ؾ߸� �ϴ� ���
        else
        {
            Button expectedButton = GameObject.Find(expectedButtonName)?.GetComponent<Button>();
            if (expectedButton == null)
            {
                Debug.LogError("��û�� ��ư�� ã�� �� �����ϴ� : " + expectedButtonName);
                yield break;
            }

            Debug.Log("��û�� ��ư�� ã�ҽ��ϴ� : " + expectedButton.name);

            float waitSeconds = 0.1f;

            // expectedButton�� transparent �г� ���� ���� ������ ��ư�� ����
            StartCoroutine(CreateCurrentOverlayAfterDelay(expectedButton, waitSeconds));

            // �� �ڷ�ƾ�� ������ ��ٸ�
            yield return new WaitForSecondsRealtime(waitSeconds + 0.01f);

            if (currentOverlay != null)
            {
                // ��ư�� ������ �߰�
                currentOverlay.onClick.AddListener(() => actionReceived = true);

                // ��ư �Է� ���
                while (!actionReceived) yield return null;

                CurrentStepFinish();
            }
            else
            {
                Debug.LogError("currentOverlay�� ���� ��� ����");
            }
        }
    }

    // ������ ��ġ�� UI�� �ƴ϶�� �̷� ������ �غ� 
    private IEnumerator CreateCurrentOverlayAfterDelay(Button targetButton, float waitSeconds)
    {
        yield return new WaitForSecondsRealtime(waitSeconds);

        CreateCurrentOverlay(targetButton);
    }

    // ��ǥ�� �ϴ� ��ư�� ������ ����� �ϴ� ������ ��ư�� ����
    // TutorialPanel�� �޹�� �гκ��� ���� ��Ұ� ������ �ϱ� ����
    private void CreateCurrentOverlay(Button targetButton)
    {
        RectTransform buttonRect = targetButton.GetComponent<RectTransform>();

        // ������ �������� ����
        GameObject overlay = new GameObject("ButtonOverlay");
        overlay.transform.SetParent(canvas.transform);

        // ���� ��ư�� ������ ��ġ, ũ��� ����
        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = buttonRect.anchorMin;
        overlayRect.anchorMax = buttonRect.anchorMax;
        overlayRect.pivot = buttonRect.pivot;
        overlayRect.position = buttonRect.position;
        overlayRect.sizeDelta = buttonRect.sizeDelta;

        // ������ �̹���
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0, 0, 0, 0); 

        // Ŭ�� �̺�Ʈ �߰�
        Button overlayButton = overlay.AddComponent<Button>();
        overlayButton.onClick.AddListener(() =>
        { 
            targetButton.GetComponent<Button>().onClick.Invoke();
        });

        Debug.Log("overlayButton�� �����ʰ� ��ϵ�");

        // ���Ÿ� ���� ���� �������� ���
        currentOverlay = overlayButton;
    }

    private Canvas? FindRootCanvas()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

        // ��Ʈ ������Ʈ ��, Canvas ������Ʈ�� ���� ������Ʈ ã��
        foreach (GameObject obj in rootObjects)
        {
            Canvas? canvas = obj.GetComponent<Canvas>();
            if (canvas != null) return canvas;
        }
        return null;
    }

    // Ʃ�丮���� ������ ����
    public void FinishAllTutorials()
    {
        Debug.Log("Ʃ�丮���� ����");
        GameManagement.Instance.PlayerDataManager.FinishAllTutorials();
    }

    // 2��° Ʃ�丮���� �̰� üũ�ؼ� ���۵�
    public void StartSecondTutorial()
    {
        bool firstTutorialCleared = GameManagement.Instance!.PlayerDataManager.IsTutorialStatus(0, PlayerDataManager.TutorialStatus.Completed);

        // 1�� Ʃ�丮���� ����, 2�� Ʃ�丮���� �÷����� �� ���� ��
        if (firstTutorialCleared)
        {
            StartSpecificTutorial(1);
            StageManager.Instance!.OnGameCleared += SaveSecondTutorialClear;
            StageManager.Instance!.OnGameFailed += SaveSecondTutorialFailed;
        }
    }

    // �������� Fail�� �� ���� ��
    public void StartSecondTutorialQuiet()
    {
        // Ʃ�丮�� ���¸� �ѵ�, ������ ����
        StartSpecificTutorial(1, false);
        StageManager.Instance!.OnGameCleared += SaveSecondTutorialClear;
        StageManager.Instance!.OnGameFailed += SaveSecondTutorialFailed;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        canvas = FindRootCanvas();
        if (canvas == null) Debug.LogError("TutorialManager�� canvas�� ���������� �Ҵ���� ����");

        // ����� �����͸� ����
        // - Ʃ�丮���� �Ϸ�/��ŵ�� ��� ����
        if (GameManagement.Instance.PlayerDataManager.IsAllTutorialFinished()) return;

        // 3��° Ʃ�丮�� ���� ����
        int lastCompletedTutorialIndex = GameManagement.Instance!.PlayerDataManager.GetLastCompletedTutorialIndex();

        if (lastCompletedTutorialIndex == 1 && // 2��° Ʃ�丮���� Ŭ����Ǿ���
            GameManagement.Instance!.PlayerDataManager.IsTutorialStatus(2, PlayerDataManager.TutorialStatus.NotStarted)) // 3��° Ʃ�丮���� ������� �ʾҴٸ�
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
