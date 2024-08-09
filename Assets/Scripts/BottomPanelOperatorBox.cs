using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BottomPanelOperatorBox : MonoBehaviour, IPointerClickHandler
{
    

    private Transform operatorIcon; // ������Ʈ
    private Image operatorIconImage; // Image ������Ʈ
    private Color originalIconColor;
    private TextMeshProUGUI costText;
    [SerializeField] private OperatorData operatorData;

    // ��ٿ� ����
    private Image cooldownImage;
    private TextMeshProUGUI cooldownText;
    private float cooldownTimer = 0f;
    private bool isOnCooldown = false;

    public void Initialize(OperatorData data)
    {
        operatorData = data;
        operatorIcon = transform.Find("OperatorIcon");
        operatorIconImage = operatorIcon.GetComponentInChildren<Image>(); // OperatorIcon�� Image ������Ʈ�� ����
        costText = transform.Find("CostBackground/CostText").GetComponent<TextMeshProUGUI>(); // CostText�� TextMeshPro�� ����

        // ��ٿ� UI ������Ʈ ã�� # transform�� �ڱ� �ڽŰ� 1�ܰ� �ڽ� ������Ʈ�� �˻���
        cooldownImage = transform.Find("CooldownOverlay").GetComponent<Image>();
        cooldownText = transform.Find("CooldownOverlay/CooldownText").GetComponent<TextMeshProUGUI>(); // �� ���� �ڽ� ������Ʈ ã��

        InitializeVisuals();


        // �ڽ�Ʈ ���� ���� ��������Ʈ�� �̺�Ʈ �߰�
        StageManager.Instance.OnDeploymentCostChanged += UpdateAvailability;
    }

    private void OnDestroy()
    {
        StageManager.Instance.OnDeploymentCostChanged -= UpdateAvailability;
    }

    private void Update()
    {
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                EndCooldown();
            }

            else
            {
                UpdateCooldownVisuals();
            }
        }
    }

    /// <summary>
    /// �ϴ��� �ʱ�ȭ ���� ���� �ֱ�� �ϴ�
    /// </summary>
    private void InitializeVisuals()
    {
        // operatorData�� �ش��ϴ� �������� �ִٸ� �װ� �ְ� 
        if (operatorData.icon != null)
        {
            operatorIconImage.sprite = operatorData.icon;
            operatorIconImage.color = new Color(1f, 1f, 1f, 1f); // �Ϸ���Ʈ�� ��ȭ�� ���� �ʰ� �״�� ����
        }

        // ���ٸ� operatorData�� �ش��ϴ� ���� �����ͼ� �Ҵ��Ѵ�.
        else if (operatorData.prefab != null)
        {
            Renderer modelRenderer = operatorData.prefab.GetComponentInChildren<Renderer>();
            if (modelRenderer != null && modelRenderer.sharedMaterial != null)
            {   
                originalIconColor = modelRenderer.sharedMaterial.color;
                operatorIconImage.color = originalIconColor;
            }
        }

        costText.text = operatorData.deploymentCost.ToString();
        //UpdateAvailability();

        cooldownImage.gameObject.SetActive(false);
        cooldownText.gameObject.SetActive(false);
    }

    // eventData : ����Ƽ�� ���� �ڵ� ����, Ŭ�� �̺�Ʈ ���� ����. (�׷��� operatorData���� ������ ��ǲ�� ������ �ۼ��ؾ� ��)
    public void OnPointerClick(PointerEventData eventData) 
    {
        if (StageManager.Instance.CurrentDeploymentCost >= operatorData.deploymentCost)
        {
            OperatorManager.Instance.StartOperatorPlacement(operatorData);
        }
    }

    public void StartCooldown(float cooldownTime)
    {
        isOnCooldown = true;
        cooldownTimer = cooldownTime;
        gameObject.SetActive(true);
        UpdateCooldownVisuals();
    }

    private void UpdateCooldownVisuals()
    {
        cooldownImage.gameObject.SetActive(true);
        cooldownText.gameObject.SetActive(true);
        cooldownImage.fillAmount = cooldownTimer / operatorData.reDeployTime;
        cooldownText.text = Mathf.Ceil(cooldownTimer).ToString();
    }

    private void EndCooldown()
    {
        isOnCooldown = false;
        cooldownImage.gameObject.SetActive(false);
        cooldownText.gameObject.SetActive(false);
    }

    private void UpdateAvailability()
    {
        bool isAvailable = StageManager.Instance.CurrentDeploymentCost >= operatorData.deploymentCost;
        Color iconColor = isAvailable ? originalIconColor : new Color(originalIconColor.r, originalIconColor.g, originalIconColor.b, 0.3f);
        operatorIconImage.color = iconColor;
        //costText.color = isAvailable ? Color.white : Color.gray;
    }
}
