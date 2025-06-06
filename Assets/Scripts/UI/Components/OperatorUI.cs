using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OperatorUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private DeployableBarUI deployableBarUI = default!;  // 기존에 할당된 Bar UI
    [SerializeField] private GameObject skillIconUI = default!;      // 스킬 아이콘 UI

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

        SetSkillIconVisibility(op.CurrentSP >= op.MaxSP);

        // 배치 시점에 카메라를 봐야 함
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }

        // 오퍼레이터 파괴 시 UI도 파괴하는 이벤트 등록
        op.OnOperatorDied += DestroyThis;
    }

    // 스킬 아이콘 UI의 초기화 및 상태 업데이트
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


    private void DestroyThis(Operator op)
    {
        Destroy(gameObject);
    }
}
