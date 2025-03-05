using UnityEngine;
using UnityEngine.EventSystems;

public class OperatorUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject deployableBarUI;  // ������ �Ҵ�� Bar UI
    [SerializeField] private GameObject skillIconUI;      // ��ų ������ UI
    [SerializeField] private SpriteRenderer directionIndicator; // ���� ǥ�ñ�

    private DeployableBarUI deployableBarUIScript;

    private Operator op;

    private Canvas canvas;
    private Camera mainCamera;

    private void Awake()
    {
        // OperatorUI�� �Ҵ�� Canvas ��������
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        mainCamera = Camera.main;
        canvas.worldCamera = mainCamera;

        // UI�� ī�޶� �������� ȸ��
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }

        directionIndicator.enabled = false;
    }

    public void Initialize(Operator op)
    {
        deployableBarUIScript = deployableBarUI.GetComponent<DeployableBarUI>();
        deployableBarUIScript.Initialize(op);
     
        this.op = op;

        SetSkillIconVisibility(op.CurrentSP >= op.MaxSP);
    }

    // ��ų ������ UI�� �ʱ�ȭ �� ���� ������Ʈ
    public void SetSkillIconVisibility(bool isVisible)
    {
        if (skillIconUI != null)
        {
            skillIconUI.SetActive(isVisible);
        }
    }

    public void UpdateUI()
    {
        if (op.IsSkillOn)
        {
            deployableBarUIScript.SetSPBarColor(GameManagement.Instance.ResourceManager.OnSkillColor);
        }
        else
        {
            deployableBarUIScript.SetSPBarColor(GameManagement.Instance.ResourceManager.OffSkillColor);
        }

        SetSkillIconVisibility(
             op.CurrentSP >= op.MaxSP && 
            !op.IsSkillOn && 
            !op.CurrentSkill.autoActivate
        );

        SetDirectionIndicator(op.FacingDirection);
    }

    public void SetDirectionIndicator(Vector3 direction)
    {
        directionIndicator.enabled = op.IsDeployed ? true : false;

        if (directionIndicator != null)
        {
            float zRot = 0f;
            if (op.FacingDirection == Vector3.left) zRot = 0;
            else if (op.FacingDirection == Vector3.right) zRot = 180;
            else if (op.FacingDirection == Vector3.forward) zRot = -90;
            else if (op.FacingDirection == Vector3.back) zRot = 90;

            directionIndicator.transform.localRotation = Quaternion.Euler(30, 0, zRot);
        }
    }
}
