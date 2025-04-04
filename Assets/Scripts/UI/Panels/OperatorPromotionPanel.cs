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
    [SerializeField] private GameObject attackRangeContents = default!; // attackRange 관련 요소들이 들어가 있음
    [SerializeField] private Image attackRangeBox = default!;
    [SerializeField] private Image skillIconImage = default!;
    [SerializeField] private TextMeshProUGUI skillNameText = default!;
    [SerializeField] private TextMeshProUGUI skillDetailText = default!;

    [SerializeField] private TextMeshProUGUI cannotConditionText = default!;

    private OwnedOperator? op;
    private OperatorData? opData;

    private string updateColor = string.Empty; // 사용 중임.

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        updateColor = GameManagement.Instance!.ResourceManager.TextUpdateColor;

        // 공격 범위 시각화 도우미 초기화
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

        // 보상 목록 초기화
        InitializeRequiredItems();

        UpdateUI();
    }

    private void UpdateUI()
    {
        // 텍스트 업데이트
        OperatorGrowthSystem.ElitePhase currentPhase = op.currentPhase;
        OperatorGrowthSystem.ElitePhase newPhase = op.currentPhase + 1; // enum이니까
        currentPromotionText.text = $"<size=200>{(int)currentPhase}</size>\n\n현재 정예화";
        newPromotionText.text = $"<size=200>{(int)newPhase}</size>\n\n목표 정예화";

        // 이미지 업데이트
        OperatorIconHelper.SetElitePhaseIcon(currentPromotionImage, currentPhase);
        OperatorIconHelper.SetElitePhaseIcon(newPromotionImage, newPhase);

        // 버튼 업데이트
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


    // 새로 업데이트되는 스킬 정보 업데이트 
    private void ShowNewSkill()
    {
        BaseSkill? unlockedSkill = opData.elite1Unlocks.unlockedSkill;

        if (unlockedSkill != null)
        {
            // 스킬 이름 정보
            skillNameText.text = $"- 스킬 사용 가능 : <color=#179bff>{unlockedSkill.skillName}</color>";

            // 스킬 아이콘 정보
            skillIconImage.sprite = unlockedSkill.skillIcon;

            // 스킬 설명
            skillDetailText.text = unlockedSkill.description;

            // 범위가 있는 스킬의 경우 스킬 범위 오브젝트를 활성화하고 보여줌
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

        // 정예화 후 추가되는 공격 범위
        List<Vector2Int>? additionalTiles = opData.elite1Unlocks.additionalAttackTiles;

        // 추가되는 범위가 없으면 공격 범위를 담는 부분은 보여주지 않음
        if (additionalTiles.Count == 0)
        {
            attackRangeContents.SetActive(false);
            return;
        }

        attackRangeContents.SetActive(true);

        // 정예화 이전 기본 공격 범위
        List<Vector2Int> baseTiles = new List<Vector2Int>(op.CurrentAttackableGridPos);

        // 기본 범위와 추가 범위를 다른 색상으로 표시
        if (additionalTiles.Count > 0)
        {
            attackRangeHelper.ShowRangeWithUnlocks(baseTiles, additionalTiles);
        }
    }

    private void SetCannotConditionText()
    {
        string baseText = "정예화 조건을 달성하지 못했습니다.\r\n필요 조건 : <color=#FFCCCC>";

        // 레벨이 부족한 경우
        if (!op.CanPromote)
        {
            string lowLevelText = "0정예화 50레벨</color>";
            cannotConditionText.text = baseText + lowLevelText;
        }
        // 정예화 아이템이 없는 경우
        else if (opData != null && !GameManagement.Instance!.PlayerDataManager.HasPromotionItems(opData))
        {
            // 요구조건이 다 동일할 예정이라 별도로 수정하지는 않겠음.
            // 반복문을 돌려서 필요 아이템들을 하나씩 나열하는 방법이 사실 정석이겠다.
            string lowLevelText = "정예화 아이템 1개</color>";
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

            // 아이템 갯수 계산 - 가지고 있는 아이템이 요구 조건에 미달하는지 여부를 판단
            int ownedCount = GameManagement.Instance!.PlayerDataManager.GetItemCount(promotionItem.itemData.itemName);
            bool showNotEnough = ownedCount < promotionItem.count;

            Debug.Log($"ShowNotEnough : {showNotEnough}");

            // 갯수는 필요 갯수를 띄우고, 부족한 건 showNotEnough로 들어가야 함
            itemUIElements[i].gameObject.SetActive(true);
            itemUIElements[i].Initialize(promotionItem.itemData, promotionItem.count, false, false, showNotEnough);
        }
    }

    private void OnDisable()
    {
        // 패널이 비활성화될 때 공격 범위 제거
        if (attackRangeHelper != null)
        {
            attackRangeHelper.ClearTiles();
        }

        // 좌측 하단의 아이템들도 제거
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
                MainMenuManager.Instance!.ShowNotification($"{op.operatorName} 정예화 완료");
                // 디테일 패널로 돌아가기
                MainMenuManager.Instance!.ActivateAndFadeOut(
                    MainMenuManager.Instance!.PanelMap[MainMenuManager.MenuPanel.OperatorDetail],
                    gameObject
                );
            }
        }
    }
}
