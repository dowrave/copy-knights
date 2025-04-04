using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatisticItem : MonoBehaviour
{
    [SerializeField] private Image operatorIcon = default!;
    [SerializeField] private Image classIcon = default!;
    [SerializeField] private TextMeshProUGUI valueText = default!;
    [SerializeField] private Slider percentageBar = default!; // ���� ������Ʈ�� Fill �� ����
    [SerializeField] private TextMeshProUGUI percentageText = default!;
    [SerializeField] private Color damageDealtColor;
    [SerializeField] private Color damageTakenColor;
    [SerializeField] private Color healingDoneColor;
    private bool showPercentage;

    //public Operator Operator { get; private set; }
    public OperatorData OpData { get; private set; } = default!;
    private StatisticsManager.OperatorStats opStats; 
    private StatisticsManager.StatType currentStatType;
    private Image fillImage = default!; // ������ ���̸� ��Ÿ���� �̹���

    /// <summary>
    /// StatItem�� �ʱ�ȭ�ϰ� �ʿ��� �̺�Ʈ�� �����մϴ�.
    /// </summary>
    public void Initialize(OperatorData opData, StatisticsManager.StatType statType, bool showPercentage)
    {
        OpData = opData;
        currentStatType = statType;

        opStats = StatisticsManager.Instance!.GetAllOperatorStats().Find(s => s.opData == opData);
        fillImage = percentageBar.fillRect.GetComponent<Image>();

        SetIcons(opData);
        SetBarColor(statType);
        SetDisplayMode(showPercentage); // ���� ��ġ / ����� ��ȯ

        // 1������ ��� �Ŀ� ������Ʈ ����(percentage ���� �ݿ� ���� ������ ����)
        StartCoroutine(DelayedUpdate(statType, showPercentage));
    }

    // ���۷����Ϳ� Ŭ���� �������� �Ҵ���
    private void SetIcons(OperatorData opData)
    {
        // ���۷����� ���� ������ �Ҵ�, ���ٸ� �� ��Ƽ������ ���� ������
        if (opData.Icon != null)
        {
            operatorIcon.sprite = opData.Icon;
        }
        else
        {
            operatorIcon.color = opData.prefab.GetComponentInChildren<Renderer>().sharedMaterial.color;
        }

        // Ŭ���� ������ �Ҵ�, ���ٸ� ���������� ǥ��
        Sprite? classSprite = GameManagement.Instance.ResourceManager.IconData.GetClassIcon(opData.operatorClass);
        if (classSprite != null)
        {
            classIcon.sprite = classSprite;
        }
        else
        {
            classIcon.color = new Color(0, 0, 0, 0);
        }
    }

    private void SetDisplayMode(bool showPercentage)
    {
        valueText.gameObject.SetActive(!showPercentage);
        percentageText.gameObject.SetActive(showPercentage);
    }

    private void SetBarColor(StatisticsManager.StatType statType)
    {
        if (fillImage != null)
        {
            switch (statType)
            {
                case StatisticsManager.StatType.DamageDealt:
                    fillImage.color = damageDealtColor;
                    break;
                case StatisticsManager.StatType.DamageTaken:
                    fillImage.color = damageTakenColor;
                    break;
                case StatisticsManager.StatType.HealingDone:
                    fillImage.color = healingDoneColor;
                    break;
            }
        }
    }
    
    private IEnumerator DelayedUpdate(StatisticsManager.StatType statType, bool showPercentage)
    {
        // UI ���̾ƿ��� ������Ʈ�� ������ ���
        yield return new WaitForEndOfFrame();

        // ���� �� ������Ʈ
        UpdateDisplay(statType, showPercentage);
    }

    /// <summary>
    /// ��� ������Ʈ �̺�Ʈ�� �����Ͽ� ���÷��̸� ������Ʈ�մϴ�.
    /// </summary>
    private void OnStatUpdated(OperatorData opData, StatisticsManager.StatType statType)
    {
        if (this.OpData == opData && statType == currentStatType)
        {
            UpdateDisplay(statType, showPercentage);
        }
    }

    /// <summary>
    /// StatItem�� ���÷��̸� ������Ʈ�մϴ�.
    /// </summary>
    public void UpdateDisplay(StatisticsManager.StatType statType, bool showPercentage)
    {
        float value = StatisticsManager.Instance!.GetOperatorValueForStatType(opStats, statType);
        float totalValue = StatisticsManager.Instance!.GetTotalValueForStatType(statType);
        float percentage = (totalValue > 0) ? (value / totalValue) : 0; // ��ü �� �� �ش� ���۷����Ͱ� �⿩�� ��

        fillImage.fillAmount = percentage;
        if (showPercentage)
        {
            SetPercentage(percentage);
        }
        else
        {
            SetValue(value);
        }

        SetDisplayMode(showPercentage);
        SetBarColor(statType);
    }

    private void SetValue(float value)
    {
        valueText.text = value.ToString("N0"); // ���� ������
    }

    private void SetPercentage(float percentage)
    {
        percentageBar.value = percentage;
        percentageText.text = (percentage * 100).ToString("F1") + "%";
    }
}