using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    // ���� ������ ���̱� ������ ������ ���·� ����
    [Header("References")]
    [SerializeField] private ConfirmationPanel confirmPanelPrefab = default!;
    [SerializeField] private GameObject tutorialPanelPrefab = default!;
    [SerializeField] private TutorialData tutorialData;

    private int currentStepIndex = -1;
    private bool isTutorialActive = false;
    private TutorialPanel? currentTutorialPanel;
    private Button? currentOverlay; // ���� Step�� ��ǥ ��ư ���� ��Ÿ���� ������ ��ư

    private ConfirmationPanel confirmPanelInstance;

    private Canvas? canvas;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // Ʃ�丮�� ���� ���θ� ���� �г�
        if (!CheckTutorialFinished())
        {
            InitializeTutorialConfirmPanel();
        }
        else
        {
            Debug.LogError("PlayerData, Ȥ�� TutorialData�� ���������� �ʱ�ȭ���� �ʾ���");
        }
    }



    // Ʃ�丮�� ���� ���θ� �˻�
    private bool CheckTutorialFinished()
    {
        return GameManagement.Instance!.PlayerDataManager.IsTutorialFinished();
    }

    private void InitializeTutorialConfirmPanel()
    {
        Canvas canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
        confirmPanelInstance = Instantiate(confirmPanelPrefab, canvas.transform);
        confirmPanelInstance.Initialize("���� ������ �����Ǿ����ϴ�. Ʃ�丮���� �����Ͻðڽ��ϱ�?", true, false);
        confirmPanelInstance.OnConfirm += CheckStartTutorial;
        confirmPanelInstance.OnCancel += CheckStopTutorial;
    }


    private void CheckStartTutorial()
    {
        confirmPanelInstance.OnConfirm -= CheckStartTutorial;
        confirmPanelInstance.OnCancel -= CheckStopTutorial;

        StartCoroutine(ShowTutorialStartPanel("Ʃ�丮���� �����մϴ�.", false, true));
    }

    private void CheckStopTutorial()
    {
        confirmPanelInstance.OnConfirm -= CheckStartTutorial;
        confirmPanelInstance.OnCancel -= CheckStopTutorial;
    }

    private IEnumerator ShowTutorialStartPanel(string message, bool isCancelButton, bool blurAreaActivation)
    {
        // �г� ��ȯ�� ���� �ణ�� ����
        yield return new WaitForSeconds(0.1f);

        // ������ �ν��Ͻ��� �� �޽��� ǥ��
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
        // ���ܸ��� ������ ����Ǿ�� �� ���
        if (currentOverlay != null)
        {
            // �Է� �� ����
            currentOverlay.onClick.RemoveAllListeners();

            Destroy(currentOverlay.gameObject);
            currentOverlay = null;
        }
        
        // ������ �����̾��ٸ� Ʃ�丮�� ����
        if (currentStepIndex == tutorialData.steps.Count)
        {
            StopTutorial();
            return;
        }

        // �ƴ϶�� ��� ����
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

        // �ؽ�Ʈ ��� �Ϸ� �̺�Ʈ�� ����� ���� ��⿡ ���� �ڷ�ƾ �߰�
        currentTutorialPanel.OnDialogueCompleted = () =>
        {
            StartCoroutine(WaitForUserAction(currentStep.expectedButtonName));
        };

        currentTutorialPanel.Initialize(currentStep);
    }

    private void AdvanceToNextStep()
    {
        // ������ �����̾��ٸ� Ʃ�丮�� ����
        if (currentStepIndex >= tutorialData.steps.Count - 1)
        {
            StopTutorial();
            return;
        }

        // ���� �������� �ε��� �̵�
        currentStepIndex++;

        // ���� ����
        PlayCurrentStep();
    }

    // ���� ������ ���� ���¿��� ���� �������� �̵��ϱ� ��, ������� ������ ��ٸ��� �޼���
    private IEnumerator WaitForUserAction(string expectedButtonName)
    {
        bool actionReceived = false;

        // Ư�� ��ư�� Ŭ���ص� ���� �ʴ� ���
        if (expectedButtonName == string.Empty)
        {
            // tutorialPanel�� ���� ���� ���� transparentPanel�� �����ʸ� �߰���
            currentTutorialPanel.AddClickListener(() => actionReceived = true);

            // ��ư �Է� ���
            while (!actionReceived) yield return null;
            CurrentStepFinish();
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
            yield return new WaitForSeconds(waitSeconds + 0.01f);

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
        yield return new WaitForSeconds(waitSeconds);

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


    private void InitializeOverlay()
    {

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

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        canvas = FindRootCanvas();
        if (canvas == null) Debug.LogError("TutorialManager�� canvas�� ���������� �Ҵ���� ����");
    }
}
