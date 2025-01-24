using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OperatorPromotionPanel : MonoBehaviour
{
    [Header("Attack Range Preview")]
    [SerializeField] private RectTransform attackRangeContainer;
    [SerializeField] private float centerPositionOffset;
    [SerializeField] private float tileSize = 25f;

    [Header("Current/Target Promotion UI")]
    [SerializeField] private TextMeshProUGUI currentPromotionText;
    [SerializeField] private TextMeshProUGUI newPromotionText;
    [SerializeField] private Image currentPromotionImage;
    [SerializeField] private Image newPromotionImage;


    private UIHelper.AttackRangeHelper attackRangeHelper;

    [Header("Controls")]
    [SerializeField] private Button confirmButton;

    [Header("UnlockContents")]
    [SerializeField] private TextMeshProUGUI skillUnlockText;
    [SerializeField] private Image attackRangePrefab;

    [SerializeField] private TextMeshProUGUI cannotConditionText;

    private OwnedOperator op;
    private string updateColor; // 사용 중임.

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        updateColor = GameManagement.Instance.ResourceManager.textUpdateColor;
    }

    private void Start()
    {
        // 공격 범위 시각화 도우미 초기화
        if (attackRangeContainer != null)
        {
            attackRangeHelper = UIHelper.Instance.CreateAttackRangeHelper(
                attackRangeContainer,
                centerPositionOffset,
                tileSize
            );
        }
    }

    public void Initialize(OwnedOperator op)
    {
        this.op = op;
        UpdatePromotionInfo();
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
        bool canPromote = op.CanPromote;
        confirmButton.interactable = canPromote;
        cannotConditionText.gameObject.SetActive(!canPromote);
    }

    private void UpdatePromotionInfo()
    {
        ShowNewSkill();
        ShowAttackRangePreview();
    }


    // 새로 업데이트되는 스킬 정보
    private void ShowNewSkill()
    {
        
    }

    private void ShowAttackRangePreview()
    {
        if (attackRangeHelper == null || op == null) return;

        // 정예화 이전 기본 공격 범위
        List<Vector2Int> baseTiles = new List<Vector2Int>(op.CurrentAttackableTiles);

        // 정예화 후 추가되는 공격 범위
        List<Vector2Int> additionalTiles = op.BaseData.elite1Unlocks.additionalAttackTiles;

        // 기본 범위와 추가 범위를 다른 색상으로 표시
        attackRangeHelper.ShowRangeWithUnlocks(baseTiles, additionalTiles);
    }

    private void OnDisable()
    {
        // 패널이 비활성화될 때 공격 범위 제거
        if (attackRangeHelper != null)
        {
            attackRangeHelper.ClearTiles();
        }
    }

    private void OnConfirmButtonClicked()
    {
        if (op.CanPromote)
        {
            bool success = OperatorGrowthManager.Instance.TryPromoteOperator(op);
            if (success)
            {
                MainMenuManager.Instance.ShowNotification($"{op.operatorName} 정예화 완료");
                // 디테일 패널로 돌아가기
                MainMenuManager.Instance.ActivateAndFadeOut(
                    MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.OperatorDetail],
                    gameObject
                );
            }
        }
    }
}
