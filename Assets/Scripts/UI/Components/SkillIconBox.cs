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

    [Header("No Skill Sprite")]
    [SerializeField] private Sprite noSkillIconSprite;

    private BaseSkill skillData;

    public event Action OnButtonClicked;

    private void Awake()
    {
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

    // SkillData가 할당되지 않은 상태로 수정
    public void ResetSkillIcon()
    {
        skillIconImage.sprite = noSkillIconSprite;
        durationBox.gameObject.SetActive(false);
        durationText.text = string.Empty;

        skillNameText.text = "";
        skillNameText.gameObject.SetActive(false);
    }

    public void SetButtonInteractable(bool interactable)
    {
        button.interactable = interactable;
    }

    // "아무것도 할당되지 않은 초기화"와, "무언가가 할당되어야 하는 초기화"를 구분함
    // 전자는 ResetSkillIcon, 후자는 Initialize
    private void OnEnable()
    {
        ResetSkillIcon();
    }

    private void OnDisable()
    {
        ResetSkillIcon();
    }
}
