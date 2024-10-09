using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    // ü�¹� UI ����
    private Slider slider;
    [SerializeField] private Image fillImage; // HealthBar�� ����� �����ϱ� ���� ������Ʈ�� ����

    private void Awake()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }

        if (fillImage == null)
        {
            fillImage = transform.Find("Fill Area/Fill").GetComponent<Image>();

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
        return fillImage != null ? fillImage.color : Color.white;
    }

    public void SetColor(Color color)
    {
        if (fillImage != null)
        {
            fillImage.color = color;
        }
    }
}
