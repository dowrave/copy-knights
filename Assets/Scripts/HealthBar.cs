using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    // ü�¹� UI ����
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
    /// ü�� ������ ������Ʈ(���� ü��, �ִ� ü��)
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
