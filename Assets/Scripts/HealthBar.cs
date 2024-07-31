using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    // 체력바 UI 관리
    [SerializeField] private Image fillImage;
    private Slider slider;

    private void Awake()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }
    }

    /// <summary>
    /// 체력 게이지 업데이트(현재 체력, 최대 체력)
    /// </summary>
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        //fillImage.fillAmount = currentHealth / maxHealth;
        if (slider != null)
        {
            slider.maxValue = maxHealth;
            slider.value = currentHealth;
        }
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    public void SetColor(Color color)
    {
        fillImage.color = color;
    }
}
