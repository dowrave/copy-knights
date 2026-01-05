using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OperatorUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private DeployableBarUI deployableBarUI = default!;  // 기존에 할당된 Bar UI
    [SerializeField] private GameObject skillIconUI = default!; // 스킬 아이콘 UI

    private Operator op = default!;
    public DeployableBarUI DeployableBarUI => deployableBarUI;

    private Canvas canvas = default!;
    private Camera mainCamera = default!;


    private void Awake()
    {
        // OperatorUI에 할당된 Canvas 가져오기
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

        // UI의 초기 모습 설정
        HandleHealthChanged(op.Health.CurrentHealth, op.Health.MaxHealth, op.Health.CurrentShield);
        HandleSPChanged(op.CurrentSP, op.MaxSP);
        HandleSkillStateChanged();

        // 배치 시점에 카메라를 봐야 함
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }

        Logger.Log("OperatorUI 초기화됨");
    }

    private void SubscribeEvents()
    {
        if (op == null) return;

        op.Health.OnHealthChanged += HandleHealthChanged;
        op.OnSPChanged += HandleSPChanged;
        op.OnSkillStateChanged += HandleSkillStateChanged;
        op.Buff.OnBuffChanged += HandleBuffChanged;
    }

    private void UnsubscribeEvents()
    {
        if (op == null) return;

        op.Health.OnHealthChanged -= HandleHealthChanged;
        op.OnSPChanged -= HandleSPChanged;
        op.OnSkillStateChanged -= HandleSkillStateChanged;
        op.Buff.OnBuffChanged -= HandleBuffChanged;
    }

    // 오퍼레이터 스킬 활성화 상태변화 이벤트 구독 메서드
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

    // 오퍼레이터 HP 변화 이벤트 구독 메서드
    private void HandleHealthChanged(float currentHealth, float maxHealth, float currentShield)
    {
        DeployableBarUI.UpdateHealthBar(currentHealth, maxHealth, currentShield);
    }

    // 오퍼레이터 SP 변화 이벤트 구독 메서드
    private void HandleSPChanged(float currentSP, float maxSP)
    {
        DeployableBarUI.UpdateSPBar(currentSP, maxSP);

        SetSkillIconVisibility(
             op.CurrentSP >= op.MaxSP &&
            !op.IsSkillOn &&
            !op.CurrentSkill.autoActivate
        );
    }

    // 스킬 아이콘 UI의 초기화 및 상태 업데이트
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
            if (isAdded) // 버프가 추가됐을 때 
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

    // SP Bar를 탄환 모드로 전환
    public void SwitchSPBarToAmmoMode(int maxAmmo, int currentAmmo)
    {
        DeployableBarUI.SwitchSPBarToAmmoMode(maxAmmo, currentAmmo);
    }

    // 탄환 모드에서 원래 모드로 전환
    public void SwitchSPBarToNormalMode()
    {
        DeployableBarUI.SwitchSPBarToNormalMode();
    }

    // 탄환 업데이트
    public void UpdateAmmoDisplay(int currentAmmo)
    {
        DeployableBarUI.UpdateAmmoDisplay(currentAmmo);
    }


    private void OnDestroy()
    {
        UnsubscribeEvents();
    }
}
