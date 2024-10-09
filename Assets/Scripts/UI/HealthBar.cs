using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    // 체력바 UI 관리
    private Slider slider;
    [SerializeField] private Image fillImage; // HealthBar의 색깔로 접근하기 위한 컴포넌트를 설정

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
