using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    // 체력바 UI 관리
    private Slider slider;
    private Image fillImage;

    private void Awake()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
            fillImage = GetComponent<Image>();
        }
    }

    /// <summary>
    /// 체력 게이지 업데이트(현재 체력, 최대 체력)
    /// </summary>
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        slider.maxValue = maxHealth;
        slider.value = currentHealth;
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    public Color GetColor()
    {
        if (fillImage != null)
        {
            return fillImage.color;
        }
    }

    public void SetColor(Color color)
    {
        if (fillImage != null)
        {
            fillImage.color = color;
        }
    }
}
