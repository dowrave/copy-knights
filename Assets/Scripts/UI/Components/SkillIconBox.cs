using Skills.Base;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;


public class SkillIconBox : MonoBehaviour
{
    [SerializeField] private Image skillIconImage = default!;
    [SerializeField] private Image durationBox = default!;
    [SerializeField] private TextMeshProUGUI durationText = default!;
    [SerializeField] private Button button = default!;
    [SerializeField] private TextMeshProUGUI skillNameText = default!;

    private BaseSkill skillData;
    private Sprite noSkillIconImage;

    public event Action OnButtonClicked;

    private void Awake()
    {
        // 인스펙터에서 기본으로 할당되어 있음
        noSkillIconImage = skillIconImage.sprite;

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        button.onClick.AddListener(HandleButtonClick);
    }

    public void Initialize(BaseSkill skillData, bool showDurationBox = false, bool showSkillName = false)
    {
        this.skillData = skillData;

        if (skillData.skillIcon != null)
        {
            skillIconImage.sprite = skillData.skillIcon;
        }
        else
        {
            throw new InvalidOperationException("스킬 아이콘이 null임!!");
        }

        if (showDurationBox &&
            skillData is ActiveSkill activeSkill &&
            activeSkill.duration > 0)
        {
            durationBox.gameObject.SetActive(true);
            durationText.text = $"{activeSkill.duration}s";
        }
        else
        {
            durationBox.gameObject.SetActive(false);
        }

        // ContentSizeFitter에 너비 변화 반영
        LayoutRebuilder.ForceRebuildLayoutImmediate(durationBox.rectTransform);

        if (showSkillName)
        {
            skillNameText.gameObject.SetActive(true);
            skillNameText.text = skillData.skillName;
        }
        else
        {
            skillNameText.gameObject.SetActive(false);
        }
    }

    private void HandleButtonClick()
    {
        OnButtonClicked?.Invoke();
    }

    public void ResetSkillIcon()
    {
        skillIconImage.sprite = noSkillIconImage;
        durationBox.gameObject.SetActive(false);
        durationText.text = string.Empty;

        skillNameText.text = "";
        skillNameText.gameObject.SetActive(false);
    }

    public void SetButtonInteractable(bool interactable)
    {
        button.interactable = interactable;
    }

    private void OnDisable()
    {
        ResetSkillIcon();
    }
}
