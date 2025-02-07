using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeployableBox : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image operatorIllustImage;
    [SerializeField] private GameObject operatorClassIconBox; // 클래스 아이콘의 부모 오브젝트
    [SerializeField] private Image operatorClassIconImage; // 아이콘 자체 할당
    [SerializeField] private Image inActiveImage;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private TextMeshProUGUI countText;

    private Sprite boxIcon;
    private GameObject deployablePrefab;
    private DeployableUnitEntity deployableComponent;
    private DeployableManager.DeployableInfo deployableInfo;
    private DeployableUnitState deployableUnitState;

    // 남은 갯수
    private bool isDragging = false;
    private int currentDeploymentCost;

    public void Initialize(DeployableManager.DeployableInfo info)
    {
        deployableInfo = info;
        deployablePrefab = info.prefab;
        deployableComponent = deployablePrefab.GetComponent<DeployableUnitEntity>(); // 다형성 활용
        deployableUnitState = DeployableManager.Instance.UnitStates[deployableInfo];

        if (deployableComponent is Operator)
        {
            OwnedOperator op = deployableInfo.ownedOperator;
            currentDeploymentCost = op.BaseData.stats.DeploymentCost; // 초기 배치 코스트 설정
            OperatorIconHelper.SetClassIcon(operatorClassIconImage, op.BaseData.operatorClass); // 클래스 아이콘 설정
            boxIcon = op.BaseData.Icon;
        }
        else
        {
            currentDeploymentCost = deployableInfo.deployableUnitData.stats.DeploymentCost;
            operatorClassIconBox.gameObject.SetActive(false);
            boxIcon = deployableInfo.deployableUnitData.Icon;
        }

        StageManager.Instance.OnDeploymentCostChanged += UpdateAvailability;
        StageManager.Instance.OnPreparationComplete += InitializeVisuals;
        InitializeVisuals();
    }

    public void UpdateDisplay(DeployableUnitState unitState)
    {
        costText.text = unitState.CurrentDeploymentCost.ToString();

        // 오퍼레이터가 아니라면 배치 가능 횟수 표시
        if (!unitState.IsOperator)
        {
            countText.gameObject.SetActive(true);
            countText.text = $"x{unitState.RemainingDeployCount}";
        }
        else
        {
            countText.gameObject.SetActive(false);
        }

        UpdateAvailability();

        // 쿨타임 상태 표시
        if (unitState.IsOnCooldown)
        {
            cooldownText.gameObject.SetActive(true);
            cooldownText.text = Mathf.Ceil(unitState.CooldownTimer).ToString();
        }
        else
        {
            cooldownText.gameObject.SetActive(false);
        }
    }


    private void Update()
    {
        if (deployableUnitState.IsOnCooldown)
        {
            deployableUnitState.UpdateCooldown();
            UpdateDisplay(deployableUnitState);
        }
    }

    // 일단은 초기화 때만 쓰고 있기는 하다
    private void InitializeVisuals()
    {
        // 일러스트가 있다면 박스 아이콘으로 사용
        if (boxIcon != null)
        {
            operatorIllustImage.sprite = boxIcon;
            operatorIllustImage.color = Color.white; // 원래의 아이콘 그대로 나타내기 위함
        }

        // 아니라면 deployable 모델의 설정을 가져옴
        else if (deployablePrefab != null)
        {
            Renderer modelRenderer = deployablePrefab.GetComponentInChildren<Renderer>();
            if (modelRenderer != null && modelRenderer.sharedMaterial != null)
            {
                operatorIllustImage.color = modelRenderer.sharedMaterial.color;
            }
        }

        UpdateDisplay(deployableUnitState);
    }

    private void UpdateAvailability()
    {
        if (CanInteract())
        {
            inActiveImage.gameObject.SetActive(false); // 흐릿한 이미지 제거
        }
        else
        {
            inActiveImage.gameObject.SetActive(true); // 흐릿한 이미지 활성화
        }
    }

    // 마우스 동작 관련 : 동작하지 않는다면 상속을 확인하라
    // 마우스 버튼을 눌렀다가 같은 위치에서 뗐을 때 발생
    public void OnPointerDown(PointerEventData eventData)
    {
        if (CanInteract())
        {
            DeployableManager.Instance.StartDeployableSelection(deployableInfo);
        }
    }

    // 버튼을 누르고 약간의 움직임이 있을 때 발생
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CanInteract())
        {
            isDragging = true;
            DeployableManager.Instance.StartDragging(deployableInfo);
        }
    }

    // 버튼을 누른 상태에서 이동할 때 프레임마다 발생
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && CanInteract())
        {
            DeployableManager.Instance.HandleDragging(deployableInfo);
        }
    }

    // 드래그 중 마우스 버튼을 뗄 때 발생 
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            DeployableManager.Instance.EndDragging(deployablePrefab);
            isDragging = false;
        }
    }

    private bool CanInteract()
    {
        return !deployableUnitState.IsOnCooldown && 
            StageManager.Instance.CurrentDeploymentCost >= currentDeploymentCost;
    }

    private void OnDestroy()
    {
        StageManager.Instance.OnPreparationComplete -= InitializeVisuals;
        StageManager.Instance.OnDeploymentCostChanged -= UpdateAvailability;
    }

}

#nullable enable