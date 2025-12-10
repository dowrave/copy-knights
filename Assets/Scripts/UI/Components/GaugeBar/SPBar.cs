using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SPBar : MonoBehaviour
{
    // 체력바 UI 관리
    [SerializeField] private Slider slider = default!;
    [SerializeField] private Image spFill = default!;
    [SerializeField] private HorizontalLayoutGroup ammoContainer = default!; // 탄환 UI를 위한 Transform

    private List<Image> ammoImages = new List<Image>();

    // 색상 설정
    private Color onSkillColor;
    private Color offSkillColor;
    private Color emptyAmmoColor = new Color(1f, 1f, 1f, 0f);
    private Color currentColor;

    // 현재 SP 게이지의 수치값
    private float currentAmount;
    private float maxAmount; // Health, SP 등 게이지가 주로 나타내는 값의 최대 수치

    private bool isAmmoMode = false;
    public bool IsAmmoMode => isAmmoMode;
    private int currentAmmoCount = 0;
    private int maxAmmoCount = 0;


    void Awake()
    {
        ResourceManager resourceManager = GameManagement.Instance!.ResourceManager;
        if (resourceManager != null)
        {
            onSkillColor = resourceManager.OnSkillColor;
            offSkillColor = resourceManager.OffSkillColor;
            currentColor = offSkillColor; // 초기 색상 설정
        }

        // 디폴트 : spFill이 켜져 있고 ammoContianer가 비활성화 상태
        spFill.gameObject.SetActive(true);
        ammoContainer.gameObject.SetActive(false);
    }

    public void UpdateSPFill(float newValue, float maxValue)
    {
        maxAmount = maxValue;
        currentAmount = newValue;

        if (slider != null)
        {
            slider.maxValue = maxAmount;
            slider.value = currentAmount;
        }

        // 이전 값 저장 후 갱신

        // 메인 게이지 비율 갱신
        float valueRatio = newValue / maxAmount;
        spFill.fillAmount = valueRatio;
    }

    public void SwitchToAmmoMode(int maxAmmo, int currentAmmo)
    {
        isAmmoMode = true;
        maxAmmoCount = maxAmmo;
        currentAmmoCount = currentAmmo;

        if (slider != null) slider.enabled = false;

        spFill.gameObject.SetActive(false);
        ammoContainer.gameObject.SetActive(true);

        CreateAmmoImages(maxAmmoCount);
        UpdateAmmoDisplay();
    }

    private void CreateAmmoImages(int maxAmmoCount)
    {
        // 기존 탄환 이미지 제거
        foreach (var image in ammoImages)
        {
            Destroy(image.gameObject);
        }
        ammoImages.Clear();

        // 새로운 탄환 이미지 생성
        for (int i = 0; i < maxAmmoCount; i++)
        {
            // 새로운 탄환 이미지 게임오브젝트 생성
            GameObject ammoImageObj = new GameObject($"AmmoImage_{i}");
            ammoImageObj.transform.SetParent(ammoContainer.transform, false);

            // Image 컴포넌트 추가
            Image ammoImage = ammoImageObj.AddComponent<Image>();

            // 이 부분 수정 필요해보임) HoriozontalLayoutGroup에서 자동으로 크기가 조정되면 계속 화면을 가득 채움
            // 탄환 이미지의 너비, 높이 설정 : Control Child Size, Child Force Expand에서 적용
            // 탄환 간 간격 설정 : Spacing에서 적용

            // 이미지 색상 설정 (현재 탄환 수에 따라)
            ammoImage.color = onSkillColor;

            // 리스트에 추가
            ammoImages.Add(ammoImage);
        }
    }

    // 탄환 이미지 제거
    private void RemoveAmmoImages()
    {
        foreach (var image in ammoImages)
        {
            Destroy(image.gameObject);
        }
        ammoImages.Clear();
    }

    public void UpdateAmmoCount(int currentAmmo)
    {
        if (!isAmmoMode) return;

        currentAmmoCount = currentAmmo;
        UpdateAmmoDisplay();
    }

    private void UpdateAmmoDisplay()
    {
        if (maxAmmoCount <= 0) return;

        for (int i = 0; i < ammoImages.Count; i++)
        {
            if (i < currentAmmoCount)
            {
                // 현재 탄환 수에 해당하는 이미지 활성화
                // ammoImages[i].gameObject.SetActive(true);
                // 현재 탄환 수에 해당하는 색깔 반영
                ammoImages[i].color = onSkillColor; // 활성화된 탄환 색상 설정
            }
            else
            {
                // 남은 탄환은 비활성화
                // ammoImages[i].gameObject.SetActive(false);
                // onSkillColor의 알파를 0으로 만듦
                ammoImages[i].color = emptyAmmoColor;

            }
        }
    }

    public void SwitchToNormalMode()
    {
        RemoveAmmoImages();
        currentAmmoCount = 0;
        maxAmmoCount = 0;
        isAmmoMode = false;

        if (slider != null) slider.enabled = true;

        // 스킬 게이지로 전환
        ammoContainer.gameObject.SetActive(false);
        spFill.gameObject.SetActive(true);

        spFill.color = offSkillColor;

        // spFill 원래 상태로 복원
        // slider를 쓰면 자체적으로 계산하므로 필요없어짐
        // spFill.fillAmount = currentAmount / maxAmount;
        // spFill.rectTransform.anchorMin = new Vector2(0, 0);
        // spFill.rectTransform.anchorMax = new Vector2(currentAmount / maxAmount, 1);
    }

    public Color GetColor()
    {
        return spFill != null ? spFill.color : Color.white;
    }
    
    public void SetColor(Color color)
    {
        if (spFill != null)
        {
            spFill.color = color;
        }
    }
}
