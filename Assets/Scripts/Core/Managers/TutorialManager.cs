using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    // DontDestroyOnLoad�� ���Ƿ� ���������� �����ϴ� �� ���� �����ϴ�
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
            Debug.LogError("PlayerData, Ȥ�� TutorialData�� ���������� �ʱ�ȭ���� �ʾ���");
        }
    }

    private void InitializeTutorialConfirmPanel()
    {
        Canvas canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
        confirmPanelInstance = Instantiate(confirmPanelPrefab, canvas.transform);
        confirmPanelInstance.Initialize("���� �������� �����Ǿ����ϴ�. Ʃ�丮���� �����Ͻðڽ��ϱ�?", true, false);
        confirmPanelInstance.OnConfirm += CheckStartTutorial;
        confirmPanelInstance.OnCancel += CheckStopTutorial;
    }


    private void CheckStartTutorial()
    {
        confirmPanelInstance.OnConfirm -= CheckStartTutorial;
        confirmPanelInstance.OnCancel -= CheckStopTutorial;

        StartCoroutine(ShowNextPanel("Ʃ�丮���� �����մϴ�.", false, true));

        // Ʃ�丮�� ���� ���� �ۼ�
    }

    private void CheckStopTutorial()
    {
        confirmPanelInstance.OnConfirm -= CheckStartTutorial;
        confirmPanelInstance.OnCancel -= CheckStopTutorial;

        StartCoroutine(ShowNextPanel("Ʃ�丮���� �������� �ʽ��ϴ�.", false, true));
    }

    private IEnumerator ShowNextPanel(string message, bool isCancelButton, bool blurAreaActivation)
    {
        // �г� ��ȯ�� ���� �ణ�� ����
        yield return new WaitForSeconds(0.2f);

        // ������ �ν��Ͻ��� �� �޽��� ǥ��
        confirmPanelInstance.Initialize(message, isCancelButton, blurAreaActivation);
    }
}
