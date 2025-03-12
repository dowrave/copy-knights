using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    // DontDestroyOnLoad�� ���Ƿ� ���������� �����ϴ� �� ���� �����ϴ�
    [Header("UI References")]
    [SerializeField] private ConfirmationPanel confirmPanelPrefab = default!;
    [SerializeField] private GameObject dialogueBoxPrefab = default!;
    [SerializeField] private TutorialData tutorialData;

    private int currentStepIndex = -1;
    private bool isTutorialActive = false;
    private DialogueBox? dialogueBox;


    private ConfirmationPanel confirmPanelInstance;

    private void Awake()
    {
        
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
        currentStepIndex = -1;

        AdvanceToNextStep();
    }

    public void StopTutorial()
    {
        isTutorialActive = false;
        dialogueBoxPrefab.SetActive(false);
    }

    public void CurrentStepFinish()
    {
        // ���ܸ��� ������ ���� ����� �� �ٸ� �� ����. �׷� ��Ұ� �߰��ž� �� ��.

        
        // ������ �����̾��ٸ� Ʃ�丮�� ����
        if (currentStepIndex == tutorialData.steps.Count)
        {
            StopTutorial();
            return;
        }

        // �ƴ϶�� ��� ����
        AdvanceToNextStep();
    }

    private void AdvanceToNextStep()
    {
        // ������ �����̾��ٸ� Ʃ�丮�� ����
        if (currentStepIndex >= tutorialData.steps.Count)
        {
            StopTutorial();
            return;
        }

        // ���ο� ���� ����
        currentStepIndex++;


        TutorialData.TutorialStep currentStep = tutorialData.steps[currentStepIndex];

        if (dialogueBox == null)
        {
            Canvas canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
            dialogueBox = Instantiate(dialogueBoxPrefab, canvas.transform).GetComponent<DialogueBox>();
        }

        // �ؽ�Ʈ ��� �Ϸ� �̺�Ʈ�� ����� ���� ��⿡ ���� �ڷ�ƾ �߰�
        dialogueBox.OnDialogueCompleted = () =>
        {
            // ����� �׼��� �ʿ��� ���, �ش� �׼��� ��ٸ�
            if (currentStep.requireUserAction)
            {
                StartCoroutine(WaitForUserAction(currentStep.expectedButtonName));
            }
            else
            {
                AdvanceToNextStep();
            }
        };

        dialogueBox.Initialize(currentStep);

    }

    private IEnumerator WaitForUserAction(string expectedButtonName)
    {
        // �ٸ� ������ �����ϱ� ���� �г� �߰�
        //Canvas canvas = FindObjectOfType<Canvas>();
        //GameObject blockPanel = Instantiate(blockPanelPrefab, canvas.transform);

        bool actionReceived = false;
        Button expectedButton = GameObject.Find(expectedButtonName)?.GetComponent<Button>();
        if (expectedButton == null)
        {
            Debug.LogError("��û�� ��ư�� ã�� �� �����ϴ� : " + expectedButtonName);
            yield break;
        }
        else
        {
            Debug.Log("��û�� ��ư�� ã�ҽ��ϴ� : " + expectedButton.name);
        }

        // ��ư�� ������ �߰�
        expectedButton.onClick.AddListener(() => actionReceived = true);
       
        // ��ư �Է� ���
        while (!actionReceived) yield return null;

        // RemoveAllListeners�� �³�?
        expectedButton.onClick.RemoveAllListeners();
        //Destroy(blockPanel);

        AdvanceToNextStep();
    }
}
