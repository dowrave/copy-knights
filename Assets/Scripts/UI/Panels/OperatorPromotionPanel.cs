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
    private string updateColor; // ��� ����.

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        updateColor = GameManagement.Instance.ResourceManager.textUpdateColor;
    }

    private void Start()
    {
        // ���� ���� �ð�ȭ ����� �ʱ�ȭ
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
        // �ؽ�Ʈ ������Ʈ
        OperatorGrowthSystem.ElitePhase currentPhase = op.currentPhase;
        OperatorGrowthSystem.ElitePhase newPhase = op.currentPhase + 1; // enum�̴ϱ�
        currentPromotionText.text = $"<size=200>{(int)currentPhase}</size>\n\n���� ����ȭ";
        newPromotionText.text = $"<size=200>{(int)newPhase}</size>\n\n��ǥ ����ȭ";

        // �̹��� ������Ʈ
        OperatorIconHelper.SetElitePhaseIcon(currentPromotionImage, currentPhase);
        OperatorIconHelper.SetElitePhaseIcon(newPromotionImage, newPhase);

        // ��ư ������Ʈ
        bool canPromote = op.CanPromote;
        confirmButton.interactable = canPromote;
        cannotConditionText.gameObject.SetActive(!canPromote);
    }

    private void UpdatePromotionInfo()
    {
        ShowNewSkill();
        ShowAttackRangePreview();
    }


    // ���� ������Ʈ�Ǵ� ��ų ����
    private void ShowNewSkill()
    {
        
    }

    private void ShowAttackRangePreview()
    {
        if (attackRangeHelper == null || op == null) return;

        // ����ȭ ���� �⺻ ���� ����
        List<Vector2Int> baseTiles = new List<Vector2Int>(op.CurrentAttackableTiles);

        // ����ȭ �� �߰��Ǵ� ���� ����
        List<Vector2Int> additionalTiles = op.BaseData.elite1Unlocks.additionalAttackTiles;

        // �⺻ ������ �߰� ������ �ٸ� �������� ǥ��
        attackRangeHelper.ShowRangeWithUnlocks(baseTiles, additionalTiles);
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
            bool success = OperatorGrowthManager.Instance.TryPromoteOperator(op);
            if (success)
            {
                MainMenuManager.Instance.ShowNotification($"{op.operatorName} ����ȭ �Ϸ�");
                // ������ �гη� ���ư���
                MainMenuManager.Instance.ActivateAndFadeOut(
                    MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.OperatorDetail],
                    gameObject
                );
            }
        }
    }
}
