using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeploymentCostPanel : MonoBehaviour
{
    private TextMeshProUGUI costText = default!;
    private Slider costGaugeSlider = default!;

    private void Awake()
    {
        costText = transform.Find("DeploymentCostText").GetComponent<TextMeshProUGUI>();
        costGaugeSlider = transform.Find("DeploymentCostSlider").GetComponent<Slider>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (StageManager.Instance != null)
        {
            costText.text = StageManager.Instance!.CurrentDeploymentCost.ToString();
            costGaugeSlider.value = StageManager.Instance!.CurrentCostGauge;
        }
    }

}
