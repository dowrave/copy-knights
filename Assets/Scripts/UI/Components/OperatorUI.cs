using UnityEngine;
using UnityEngine.EventSystems;

public class OperatorUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject deployableBarUI = default!;  // 기존에 할당된 Bar UI
    [SerializeField] private GameObject skillIconUI = default!;      // 스킬 아이콘 UI

    private DeployableBarUI deployableBarUIScript = default!;

    public DeployableBarUI DeployableBarUI => deployableBarUIScript;

    private Operator op = default!;

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

        deployableBarUIScript = deployableBarUI.GetComponent<DeployableBarUI>();
        deployableBarUIScript.Initialize(op);

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
        Debug.Log("operatorUI 파괴 로직 동작");
        Destroy(gameObject);
    }
}
