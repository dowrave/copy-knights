using UnityEngine;

public class OperatorUI : MonoBehaviour
{
    public GameObject deployableBarUI;  // ������ �Ҵ�� Bar UI
    private DeployableBarUI deployableBarUIScript;
    public GameObject skillIconUI;      // ��ų ������ UI

    public Operator op;
    [SerializeField] private Color originalSPBarColor;
    [SerializeField] private Color onSkillSPBarColor;



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
        // ��ų ��� ���� ���� UI
        if (op.IsSkillActive)
        {
            deployableBarUIScript.UpdateSPBar(op.RemainingSkillDuration, op.SkillDuration);
            deployableBarUIScript.SetSPBarColor(onSkillSPBarColor);
        }
        // ��ų ��� ���� �ƴ� ���� UI
        else
        {
            deployableBarUIScript.UpdateSPBar(op.CurrentSP, op.MaxSP);
            deployableBarUIScript.SetSPBarColor(originalSPBarColor); 
        }

        SetSkillIconVisibility(op.CurrentSP >= op.MaxSP && !op.IsSkillActive);
    }
}
