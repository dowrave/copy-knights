using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameWinPanel; // ����� �ִϸ��̼��� ���ϱ� �ϴ� ���α⸸ �մϴ�
    [SerializeField] private GameObject deploymentCostPanel;
    [SerializeField] private GameObject topCenterPanel; // ���� �� ��, ������ ��
    [SerializeField] private GameObject bottomPanel;
    [SerializeField] private GameObject operatorInfoPanel;
    //[SerializeField] private GameObject overlayPanel;

    private OperatorInfoPanel operatorInfoPanelScript;
    //private OverlayPanel overlayPanelScript;


    // Awake�� ��� ������Ʈ�� �ʱ�ȭ ���� ����Ǿ �ٸ� ��ũ��Ʈ�� ������ �� �ֵ��� �Ѵ�. Ư�� UI�� Awake�� �� ��.
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } 
        else
        {
            Destroy(gameObject);
        }


        // �ν����Ϳ��� �Ҵ��ϰ�����, �������� ����
        if (gameOverPanel == null)
        {
            gameOverPanel = transform.Find("GameOverPanel").gameObject;
        }
        if (gameWinPanel == null)
        {
            gameWinPanel = transform.Find("GameWinPanel").gameObject;
        }
        if (operatorInfoPanel == null)
        {
            operatorInfoPanel = transform.Find("OperatorInfoPanel").gameObject;
        }

        // ��Ȱ��ȭ ���� ������ �־�δ� �� ����
        operatorInfoPanelScript = operatorInfoPanel.GetComponent<OperatorInfoPanel>();

        // �г� ��Ȱ��ȭ
        gameOverPanel.SetActive(false);
        gameWinPanel.SetActive(false);
        operatorInfoPanel.SetActive(false);

    }

    public void ShowGameOverUI()
    {
        gameOverPanel.SetActive(true);
    }
    public void ShowGameWinUI()
    {
        gameWinPanel.SetActive(true);
    }

    public void ShowOperatorInfo(OperatorData operatorData, Operator op = null)
    {
        if (operatorInfoPanelScript != null)
        {
            operatorInfoPanel.SetActive(true);

            operatorInfoPanelScript.UpdateInfo(operatorData, op);
            CameraManager.Instance.AdjustForOperatorInfo(true, op);
        }
    }

    public void HideOperatorInfo()
    {
        if (operatorInfoPanel != null)
        {
            operatorInfoPanel.SetActive(false);
            CameraManager.Instance.AdjustForOperatorInfo(false);
        }
    }

}
