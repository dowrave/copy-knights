using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    // DontDestroyOnLoad에 들어가므로 프리팹으로 저장하는 게 제일 안전하다
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
        // 스텝마다 끝나고 나서 진행될 게 다를 것 같음. 그런 요소가 추가돼야 할 듯.

        
        // 마지막 스텝이었다면 튜토리얼 종료
        if (currentStepIndex == tutorialData.steps.Count)
        {
            StopTutorial();
            return;
        }

        // 아니라면 계속 진행
        AdvanceToNextStep();
    }

    private void AdvanceToNextStep()
    {
        // 마지막 스텝이었다면 튜토리얼 종료
        if (currentStepIndex >= tutorialData.steps.Count)
        {
            StopTutorial();
            return;
        }

        // 새로운 스텝 시작
        currentStepIndex++;


        TutorialData.TutorialStep currentStep = tutorialData.steps[currentStepIndex];

        if (dialogueBox == null)
        {
            Canvas canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
            dialogueBox = Instantiate(dialogueBoxPrefab, canvas.transform).GetComponent<DialogueBox>();
        }

        // 텍스트 출력 완료 이벤트에 사용자 동작 대기에 대한 코루틴 추가
        dialogueBox.OnDialogueCompleted = () =>
        {
            // 사용자 액션이 필요한 경우, 해당 액션을 기다림
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
        // 다른 동작을 방지하기 위한 패널 추가
        //Canvas canvas = FindObjectOfType<Canvas>();
        //GameObject blockPanel = Instantiate(blockPanelPrefab, canvas.transform);

        bool actionReceived = false;
        Button expectedButton = GameObject.Find(expectedButtonName)?.GetComponent<Button>();
        if (expectedButton == null)
        {
            Debug.LogError("요청된 버튼을 찾을 수 없습니다 : " + expectedButtonName);
            yield break;
        }
        else
        {
            Debug.Log("요청된 버튼을 찾았습니다 : " + expectedButton.name);
        }

        // 버튼에 리스너 추가
        expectedButton.onClick.AddListener(() => actionReceived = true);
       
        // 버튼 입력 대기
        while (!actionReceived) yield return null;

        // RemoveAllListeners가 맞나?
        expectedButton.onClick.RemoveAllListeners();
        //Destroy(blockPanel);

        AdvanceToNextStep();
    }
}
