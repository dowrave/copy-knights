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
        opData = op.OperatorProgressData; 

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
        bool canPromote = op.CanPromote && HasPromotionItems();

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

    private bool HasPromotionItems()
    {
        int promoItemCount = GameManagement.Instance!.PlayerDataManager.GetItemCount("ItemPromotion");

        return promoItemCount > 0;
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
        else if (!HasPromotionItems())
        {
            string lowLevelText = "����ȭ ������ 1��</color>";
            cannotConditionText.text = baseText + lowLevelText;
        }
    }


    private void OnDisable()
    {
        // �г��� ��Ȱ��ȭ�� �� ���� ���� ����
        if (attackRangeHelper != null)
        {
            attackRangeHelper.ClearTiles();
        }
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
