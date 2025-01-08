using UnityEngine;
using UnityEngine.EventSystems;

public class OperatorUI : MonoBehaviour
{
    public GameObject deployableBarUI;  // 기존에 할당된 Bar UI
    private DeployableBarUI deployableBarUIScript;
    public GameObject skillIconUI;      // 스킬 아이콘 UI

    public Operator op;
    private Color originalSPBarColor;
    [SerializeField] private Color onSkillSPBarColor;

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
    }

    public void Initialize(Operator op)
    {
        deployableBarUIScript = deployableBarUI.GetComponent<DeployableBarUI>();
        deployableBarUIScript.Initialize(op);
     
        this.op = op;
        originalSPBarColor = deployableBarUIScript.GetSPBarColor();

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
            deployableBarUIScript.SetSPBarColor(onSkillSPBarColor);
        }
        else
        {
            deployableBarUIScript.SetSPBarColor(originalSPBarColor);
        }

        SetSkillIconVisibility(op.CurrentSP >= op.MaxSP && !op.IsSkillOn);
    }
}
