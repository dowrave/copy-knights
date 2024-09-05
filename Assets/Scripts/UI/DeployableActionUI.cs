using UnityEngine;
using UnityEngine.UI;


public class DeployableActionUI : MonoBehaviour
{
    [SerializeField] private MaskedDiamondOverlay maskedOverlay;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button retreatButton;

    private float darkPanelAlpha = 0f;

    private Camera mainCamera;
    private Operator op;

    public void Initialize(IDeployable deployable)
    {
        if (deployable is Operator op)
        {
            this.op = op;
        }

        // ���۷����Ͱ� �ƴ϶�� ��ų ��ư�� ��Ȱ��ȭ
        if (this.op == null)
        {
            skillButton.gameObject.SetActive(false);
        }

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
    }

    private void SetUpButtons()
    {
        // ��ư ��ġ�� �ν����Ϳ��� ����
        // ��ư �̺�Ʈ ����
        retreatButton.onClick.AddListener(OnRetreatButtonClicked);

        if (op != null) 
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
        if (op != null)
        {
            Debug.Log("��ų ��ư Ŭ����");
            op.UseSkill();
            Hide();
        }
    }
    private void OnRetreatButtonClicked()
    {
        Debug.Log("�� ��ư Ŭ����");
        op.Retreat();
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

}