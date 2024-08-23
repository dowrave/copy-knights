
using UnityEngine;
using UnityEngine.UI;


public class OperatorInfoPanel : MonoBehaviour
{
    public static OperatorInfoPanel Instance { get; private set; }

    //[SerializeField] private RectTransform infoPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowOperatorInfo(OperatorData operatorData)
    {
        // �г� Ȱ��ȭ
        gameObject.SetActive(true);

        // ī�޶� �̵� �� ũ�� ����
        CameraManager.Instance.AdjustForOperatorInfo(true);

        // ���۷����� ���� ǥ�� -> ���� �ʿ�
    }

    public void HideOperatorInfo()
    {
        // �г� ��Ȱ��ȭ
        gameObject.SetActive(false);

        //infoPanel.gameObject.SetActive(false);

        // ī�޶� ����ġ
        CameraManager.Instance.AdjustForOperatorInfo(false);

    }

}
