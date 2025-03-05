using UnityEngine;
using UnityEngine.EventSystems;

public class OperatorUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject deployableBarUI;  // 기존에 할당된 Bar UI
    [SerializeField] private GameObject skillIconUI;      // 스킬 아이콘 UI
    [SerializeField] private SpriteRenderer directionIndicator; // 방향 표시기

    private DeployableBarUI deployableBarUIScript;

    private Operator op;

    private Canvas canvas;
    private Camera mainCamera;

    private void Awake()
    {
        // OperatorUI에 할당된 Canvas 가져오기
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        mainCamera = Camera.main;
        canvas.worldCamera = mainCamera;

        // UI를 카메라 방향으로 회전
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
