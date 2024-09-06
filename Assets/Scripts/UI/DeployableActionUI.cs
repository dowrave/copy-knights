using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class DeployableActionUI : MonoBehaviour
{
    [SerializeField] private MaskedDiamondOverlay maskedOverlay;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button retreatButton;

    private float darkPanelAlpha = 0f;

    private Camera mainCamera;
    private IDeployable deployable;

    public void Initialize(IDeployable deployable)
    {
        this.deployable = deployable;

        mainCamera = Camera.main;
        SetUpButtons();
        UpdateSkillIcon();

        // UI�� ���� ���� ����
        if (mainCamera != null)
        {
            //transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
            transform.rotation = Quaternion.Euler(90, 0, 0);
        }

        maskedOverlay.Initialize(darkPanelAlpha); // ���� 0���� ����

        gameObject.SetActive(true);

        if (IsOperator() == false)
        {
            skillButton.gameObject.SetActive(false);
        }
    }

    private void SetUpButtons()
    {
        // ��ư ��ġ�� �ν����Ϳ��� ����
        // ��ư �̺�Ʈ ����
        retreatButton.onClick.AddListener(OnRetreatButtonClicked);

        if (IsOperator()) 
        { 
            skillButton.onClick.AddListener(OnSkillButtonClicked);
        }
    }

    private void UpdateSkillIcon()
    {
        // ��ų ������ ������Ʈ ����
    }


    private void OnSkillButtonClicked()
    {
        if (deployable is Operator op)
        {
            Debug.Log("��ų ��ư Ŭ����");
            op.UseSkill();
            Hide();
        }
    }
    private void OnRetreatButtonClicked()
    {
        Debug.Log("�� ��ư Ŭ����");
        deployable.Retreat();
        Hide();
    }

    public void Show()
    {
        maskedOverlay.Show();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        maskedOverlay.Hide();
        gameObject.SetActive(false);
    }

    private bool IsOperator()
    {
        return deployable is Operator ? true : false;
    }
}