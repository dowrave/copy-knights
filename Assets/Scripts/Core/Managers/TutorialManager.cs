using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    // DontDestroyOnLoad에 들어가므로 프리팹으로 저장하는 게 제일 안전하다
    [Header("UI References")]
    [SerializeField] private ConfirmationPanel confirmPanelPrefab;

    ConfirmationPanel confirmPanelInstance;

    private void Awake()
    {
        
    }

    private void Start()
    {
        if (GameManagement.Instance!.PlayerDataManager.GetTutorialData() != null)
        {
            InitializeTutorialConfirmPanel();
        }
        else
        {
            Debug.LogError("PlayerData, 혹은 TutorialData가 정상적으로 초기화되지 않았음");
        }
    }

    private void InitializeTutorialConfirmPanel()
    {
        Canvas canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
        confirmPanelInstance = Instantiate(confirmPanelPrefab, canvas.transform);
        confirmPanelInstance.Initialize("최초 실행임이 감지되었습니다. 튜토리얼을 진행하시겠습니까?", true, false);
        confirmPanelInstance.OnConfirm += CheckStartTutorial;
        confirmPanelInstance.OnCancel += CheckStopTutorial;
    }


    private void CheckStartTutorial()
    {
        confirmPanelInstance.OnConfirm -= CheckStartTutorial;
        confirmPanelInstance.OnCancel -= CheckStopTutorial;

        StartCoroutine(ShowNextPanel("튜토리얼을 시작합니다.", false, true));

        // 튜토리얼 시작 로직 작성
    }

    private void CheckStopTutorial()
    {
        confirmPanelInstance.OnConfirm -= CheckStartTutorial;
        confirmPanelInstance.OnCancel -= CheckStopTutorial;

        StartCoroutine(ShowNextPanel("튜토리얼을 진행하지 않습니다.", false, true));
    }

    private IEnumerator ShowNextPanel(string message, bool isCancelButton, bool blurAreaActivation)
    {
        // 패널 전환을 위한 약간의 지연
        yield return new WaitForSeconds(0.2f);

        // 동일한 인스턴스로 새 메시지 표시
        confirmPanelInstance.Initialize(message, isCancelButton, blurAreaActivation);
    }
}
