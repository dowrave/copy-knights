using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    // ü�¹� UI ����
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
    /// ü�� ������ ������Ʈ(���� ü��, �ִ� ü��)
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
