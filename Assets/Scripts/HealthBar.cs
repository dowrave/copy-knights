using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    // ü�¹� UI ����
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
    /// ü�� ������ ������Ʈ(���� ü��, �ִ� ü��)
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
