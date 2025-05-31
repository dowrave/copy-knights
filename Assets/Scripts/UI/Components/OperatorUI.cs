using UnityEngine;
using UnityEngine.EventSystems;

public class OperatorUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject deployableBarUI = default!;  // ������ �Ҵ�� Bar UI
    [SerializeField] private GameObject skillIconUI = default!;      // ��ų ������ UI

    private DeployableBarUI deployableBarUIScript = default!;

    public DeployableBarUI DeployableBarUI => deployableBarUIScript;

    private Operator op = default!;

    private Canvas canvas = default!;
    private Camera mainCamera = default!;

    private void Awake()
    {
        // OperatorUI�� �Ҵ�� Canvas ��������
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        mainCamera = Camera.main;
        canvas.worldCamera = mainCamera;
    }

    public void Initialize(Operator op)
    {
        this.op = op;

        deployableBarUIScript = deployableBarUI.GetComponent<DeployableBarUI>();
        deployableBarUIScript.Initialize(op);

        transform.position = op.transform.position;

        SetSkillIconVisibility(op.CurrentSP >= op.MaxSP);

        // ��ġ ������ ī�޶� ���� ��
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }

        // ���۷����� �ı� �� UI�� �ı��ϴ� �̺�Ʈ ���
        op.OnOperatorDied += DestroyThis;
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
            deployableBarUIScript.SetSPBarColor(GameManagement.Instance!.ResourceManager.OnSkillColor);
        }
        else
        {
            deployableBarUIScript.SetSPBarColor(GameManagement.Instance!.ResourceManager.OffSkillColor);
        }

        SetSkillIconVisibility(
             op.CurrentSP >= op.MaxSP && 
            !op.IsSkillOn && 
            !op.CurrentSkill.autoActivate
        );
    }

    private void DestroyThis(Operator op)
    {
        Debug.Log("operatorUI �ı� ���� ����");
        Destroy(gameObject);
    }
}
