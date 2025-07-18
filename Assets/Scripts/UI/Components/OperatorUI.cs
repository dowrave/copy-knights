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

        UnsubscribeEvents();
        SubscribeEvents();

        // UI�� �ʱ� ��� ����
        HandleHealthChanged(op.CurrentHealth, op.MaxHealth, op.GetCurrentShield());
        HandleSPChanged(op.CurrentSP, op.MaxSP);
        HandleSkillStateChanged();

        // ��ġ ������ ī�޶� ���� ��
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }
    }

    private void SubscribeEvents()
    {
        if (op == null) return;

        op.OnHealthChanged += HandleHealthChanged;
        op.OnSPChanged += HandleSPChanged;
        op.OnSkillStateChanged += HandleSkillStateChanged;
        op.OnBuffChanged += HandleBuffChanged;
    }

    private void UnsubscribeEvents()
    {
        if (op == null) return;

        op.OnHealthChanged -= HandleHealthChanged;
        op.OnSPChanged -= HandleSPChanged;
        op.OnSkillStateChanged -= HandleSkillStateChanged;
        op.OnBuffChanged -= HandleBuffChanged;
    }

    // ���۷����� ��ų Ȱ��ȭ ���º�ȭ �̺�Ʈ ���� �޼���
    private void HandleSkillStateChanged()
    {
        if (op.IsSkillOn)
        {
            DeployableBarUI.SetSPBarColor(GameManagement.Instance!.ResourceManager.OnSkillColor);
        }
        else
        {
            DeployableBarUI.SetSPBarColor(GameManagement.Instance!.ResourceManager.OffSkillColor);
        }
    }

    // ���۷����� HP ��ȭ �̺�Ʈ ���� �޼���
    private void HandleHealthChanged(float currentHealth, float maxHealth, float currentShield)
    {
        DeployableBarUI.UpdateHealthBar(currentHealth, maxHealth, currentShield);
    }

    // ���۷����� SP ��ȭ �̺�Ʈ ���� �޼���
    private void HandleSPChanged(float currentSP, float maxSP)
    {
        DeployableBarUI.UpdateSPBar(currentSP, maxSP);

        SetSkillIconVisibility(
             op.CurrentSP >= op.MaxSP &&
            !op.IsSkillOn &&
            !op.CurrentSkill.autoActivate
        );
    }

    // ��ų ������ UI�� �ʱ�ȭ �� ���� ������Ʈ
    private void SetSkillIconVisibility(bool isVisible)
    {
        if (skillIconUI != null)
        {
            skillIconUI.SetActive(isVisible);
        }
    }

    private void HandleBuffChanged(Buff changedBuff, bool isAdded)
    {
        if (changedBuff is AttackCounterBuff counterBuff)
        {
            if (isAdded) // ������ �߰����� �� 
            {
                counterBuff.OnAmmoChanged += HandleAmmoChanged;
                SwitchSPBarToAmmoMode(counterBuff.MaxAttacks, counterBuff.CurrentAttacks);
            }
            else
            {
                counterBuff.OnAmmoChanged -= HandleAmmoChanged;
                SwitchSPBarToNormalMode();
            }
         }
    }

    private void HandleAmmoChanged(int currentAmmo, int maxAmmo)
    {
        DeployableBarUI.UpdateAmmoDisplay(currentAmmo);
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


    private void OnDestroy()
    {
        UnsubscribeEvents();
    }
}
