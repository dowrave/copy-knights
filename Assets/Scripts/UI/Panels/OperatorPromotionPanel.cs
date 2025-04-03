using System.Collections.Generic;
using Skills.Base;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class OperatorPromotionPanel : MonoBehaviour
{
    [Header("Range Container")]
    [SerializeField] private RectTransform attackRangeContainer = default!;
    private float attackRangecenterPositionOffset = 45f;
    private float attackRangeTileSize = 25f;
    [SerializeField] private RectTransform skillRangeContainer = default!;
    private float skillRangecenterPositionOffset = 0f;
    private float skillRangeTileSize = 20f;

    [Header("About Required Items")]
    [SerializeField] private Transform requiredItemList = default!;
    [SerializeField] private List<ItemUIElement> itemUIElements = default!;

    [Header("Current/Target Promotion UI")]
    [SerializeField] private TextMeshProUGUI currentPromotionText = default!;
    [SerializeField] private TextMeshProUGUI newPromotionText = default!;
    [SerializeField] private Image currentPromotionImage = default!;
    [SerializeField] private Image newPromotionImage = default!;

    private UIHelper.AttackRangeHelper attackRangeHelper = default!;
    private UIHelper.AttackRangeHelper skillRangeHelper = default!;

    [Header("Controls")]
    [SerializeField] private Button confirmButton = default!;

    [Header("Unlock Contents")]
    [SerializeField] private GameObject attackRangeContents = default!; // attackRange ���� ��ҵ��� �� ����
    [SerializeField] private Image attackRangeBox = default!;
    [SerializeField] private Image skillIconImage = default!;
    [SerializeField] private TextMeshProUGUI skillNameText = default!;
    [SerializeField] private TextMeshProUGUI skillDetailText = default!;

    [SerializeField] private TextMeshProUGUI cannotConditionText = default!;

    private OwnedOperator? op;
    private OperatorData? opData;

    private string updateColor = string.Empty; // ��� ����.

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        updateColor = GameManagement.Instance!.ResourceManager.TextUpdateColor;

        // ���� ���� �ð�ȭ ����� �ʱ�ȭ
        if (attackRangeContainer != null)
        {
            attackRangeHelper = UIHelper.Instance!.CreateAttackRangeHelper(
                attackRangeContainer,
                attackRangecenterPositionOffset,
                attackRangeTileSize
            );

            skillRangeHelper = UIHelper.Instance!.CreateAttackRangeHelper(
                skillRangeContainer,
                skillRangecenterPositionOffset,
                skillRangeTileSize
            );
        }
    }

    public void Initialize(OwnedOperator op)
    {
        this.op = op;
        opData = op.OperatorProgressData!;

        // ���� ��� �ʱ�ȭ
        InitializeRequiredItems();

        UpdateUI();
    }

    private void UpdateUI()
    {
        // �ؽ�Ʈ ������Ʈ
        OperatorGrowthSystem.ElitePhase currentPhase = op.currentPhase;
        OperatorGrowthSystem.ElitePhase newPhase = op.currentPhase + 1; // enum�̴ϱ�
        currentPromotionText.text = $"<size=200>{(int)currentPhase}</size>\n\n���� ����ȭ";
        newPromotionText.text = $"<size=200>{(int)newPhase}</size>\n\n��ǥ ����ȭ";

        // �̹��� ������Ʈ
        OperatorIconHelper.SetElitePhaseIcon(currentPromotionImage, currentPhase);
        OperatorIconHelper.SetElitePhaseIcon(newPromotionImage, newPhase);

        // ��ư ������Ʈ
        bool canPromote = op.CanPromote && GameManagement.Instance!.PlayerDataManager.HasPromotionItems(opData);

        confirmButton.interactable = canPromote;
        cannotConditionText.gameObject.SetActive(!canPromote);

        if (!canPromote)
        {
            SetCannotConditionText();
        }

        UpdatePromotionInfoUI();
    }

    private void UpdatePromotionInfoUI()
    {
        ShowNewSkill();
        ShowAttackRangePreview();
        ShowRequiredItems();
    }


    // ���� ������Ʈ�Ǵ� ��ų ���� ������Ʈ 
    private void ShowNewSkill()
    {
        BaseSkill? unlockedSkill = opData.elite1Unlocks.unlockedSkill;

        if (unlockedSkill != null)
        {
            // ��ų �̸� ����
            skillNameText.text = $"- ��ų ��� ���� : <color=#179bff>{unlockedSkill.skillName}</color>";

            // ��ų ������ ����
            skillIconImage.sprite = unlockedSkill.skillIcon;

            // ��ų ����
            skillDetailText.text = unlockedSkill.description;

            // ������ �ִ� ��ų�� ��� ��ų ���� ������Ʈ�� Ȱ��ȭ�ϰ� ������
            if (unlockedSkill is ActiveSkill unlockedActiveSkill && 
                unlockedActiveSkill.SkillRangeOffset.Count > 0)
            {
                List<Vector2Int> skillRange = new List<Vector2Int>(unlockedActiveSkill.SkillRangeOffset);
                skillRangeHelper.ShowBasicRange(skillRange, unlockedActiveSkill.RectOffset, unlockedActiveSkill.ActiveFromOperatorPosition);
                skillRangeContainer.gameObject.SetActive(true);
            }
            else
            {
                skillRangeContainer.gameObject.SetActive(false);
            }
        }
    }




    private void ShowAttackRangePreview()
    {
        if (attackRangeHelper == null) return;

        // ����ȭ �� �߰��Ǵ� ���� ����
        List<Vector2Int>? additionalTiles = opData.elite1Unlocks.additionalAttackTiles;

        // �߰��Ǵ� ������ ������ ���� ������ ��� �κ��� �������� ����
        if (additionalTiles.Count == 0)
        {
            attackRangeContents.SetActive(false);
            return;
        }

        attackRangeContents.SetActive(true);

        // ����ȭ ���� �⺻ ���� ����
        List<Vector2Int> baseTiles = new List<Vector2Int>(op.CurrentAttackableGridPos);

        // �⺻ ������ �߰� ������ �ٸ� �������� ǥ��
        if (additionalTiles.Count > 0)
        {
            attackRangeHelper.ShowRangeWithUnlocks(baseTiles, additionalTiles);
        }
    }

    private void SetCannotConditionText()
    {
        string baseText = "����ȭ ������ �޼����� ���߽��ϴ�.\r\n�ʿ� ���� : <color=#FFCCCC>";

        // ������ ������ ���
        if (!op.CanPromote)
        {
            string lowLevelText = "0����ȭ 50����</color>";
            cannotConditionText.text = baseText + lowLevelText;
        }
        // ����ȭ �������� ���� ���
        else if (opData != null && !GameManagement.Instance!.PlayerDataManager.HasPromotionItems(opData))
        {
            // �䱸������ �� ������ �����̶� ������ ���������� �ʰ���.
            // �ݺ����� ������ �ʿ� �����۵��� �ϳ��� �����ϴ� ����� ��� �����̰ڴ�.
            string lowLevelText = "����ȭ ������ 1��</color>";
            cannotConditionText.text = baseText + lowLevelText;
        }
    }

    private void InitializeRequiredItems()
    {
        for (int i = 0; i < itemUIElements.Count; i++)
        {
            itemUIElements[i].gameObject.SetActive(false);
        }
    }

    private void ShowRequiredItems()
    {
        for (int i = 0; i < opData.promotionItems.Count; i++)
        {
            OperatorData.PromotionItems promotionItem = opData.promotionItems[i];

            // ������ ���� ��� - ������ �ִ� �������� �䱸 ���ǿ� �̴��ϴ��� ���θ� �Ǵ�
            int ownedCount = GameManagement.Instance!.PlayerDataManager.GetItemCount(promotionItem.itemData.itemName);
            bool showNotEnough = ownedCount < promotionItem.count;

            Debug.Log($"ShowNotEnough : {showNotEnough}");

            // ������ �ʿ� ������ ����, ������ �� showNotEnough�� ���� ��
            itemUIElements[i].gameObject.SetActive(true);
            itemUIElements[i].Initialize(promotionItem.itemData, promotionItem.count, false, false, showNotEnough);
        }
    }

    private void OnDisable()
    {
        // �г��� ��Ȱ��ȭ�� �� ���� ���� ����
        if (attackRangeHelper != null)
        {
            attackRangeHelper.ClearTiles();
        }

        // ���� �ϴ��� �����۵鵵 ����
        //foreach (ItemUIElement itemUIElement in activeItemUIElements)
        //{
        //    Destroy(itemUIElement);
        //}
    }

    private void OnConfirmButtonClicked()
    {
        if (op.CanPromote)
        {
            bool success = OperatorGrowthManager.Instance!.TryPromoteOperator(op);
            if (success)
            {
                MainMenuManager.Instance!.ShowNotification($"{op.operatorName} ����ȭ �Ϸ�");
                // ������ �гη� ���ư���
                MainMenuManager.Instance!.ActivateAndFadeOut(
                    MainMenuManager.Instance!.PanelMap[MainMenuManager.MenuPanel.OperatorDetail],
                    gameObject
                );
            }
        }
    }
}
