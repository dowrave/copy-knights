using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    // 체력바 UI 관리
    //[SerializeField] private Image fillImage;
    private Slider slider;

    private void Awake()
    {
        //if (fillImage == null)
        //{
        //    fillImage = transform.Find("Fill Area/Fill")?.GetComponent<Image>();
        //}
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
        //if (fillImage != null)
        //{
        //    fillImage.fillAmount = currentHealth / maxHealth;

        //    Debug.Log($"HealthBar Updated - Current Health: {currentHealth}, Max Health: {maxHealth}, Fill Amount: {fillImage.fillAmount}");
        //}

        slider.maxValue = maxHealth;
        slider.value = currentHealth;

    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    public void SetColor(Color color)
    {
        
    }
}
