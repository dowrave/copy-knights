using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OperatorUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private DeployableBarUI deployableBarUI = default!;  // ������ �Ҵ�� Bar UI
    [SerializeField] private GameObject skillIconUI = default!; // ��ų ������ UI

    private Operator op = default!;
    public DeployableBarUI DeployableBarUI => deployableBarUI;

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

        DeployableBarUI.Initialize(op);

        transform.position = op.transform.position;

        SetSkillIconVisibility(op.CurrentSP >= op.MaxSP);

        // ��ġ ������ ī�޶� ���� ��
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }

        // ���۷����� �ı� �� UI�� �ı��ϴ� �̺�Ʈ ���
        // op.OnOperatorDied += DestroyThis;
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
            DeployableBarUI.SetSPBarColor(GameManagement.Instance!.ResourceManager.OnSkillColor);
        }
        else
        {
            DeployableBarUI.SetSPBarColor(GameManagement.Instance!.ResourceManager.OffSkillColor);
        }

        SetSkillIconVisibility(
             op.CurrentSP >= op.MaxSP && 
            !op.IsSkillOn && 
            !op.CurrentSkill.autoActivate
        );
    }

    // SP Bar�� źȯ ���� ��ȯ
    public void SwitchSPBarToAmmoMode(int maxAmmo, int currentAmmo)
    {
        DeployableBarUI.SwitchSPBarToAmmoMode(maxAmmo, currentAmmo);
    }

    // źȯ ��忡�� ���� ���� ��ȯ
    public void SwitchSPBarToNormalMode()
    {
        DeployableBarUI.SwitchSPBarToNormalMode();
    }

    // źȯ ������Ʈ
    public void UpdateAmmoDisplay(int currentAmmo)
    {
        DeployableBarUI.UpdateAmmoDisplay(currentAmmo);
    }


    // private void DestroyThis(Operator op)
    // {
    //     Destroy(gameObject);
    // }
}
