using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OperatorPromotionPanel : MonoBehaviour
{
    [Header("Attack Range Preview")]
    [SerializeField] private RectTransform attackRangeContainer = default!;
    [SerializeField] private float centerPositionOffset;
    [SerializeField] private float tileSize = 25f;

    [Header("Current/Target Promotion UI")]
    [SerializeField] private TextMeshProUGUI currentPromotionText = default!;
    [SerializeField] private TextMeshProUGUI newPromotionText = default!;
    [SerializeField] private Image currentPromotionImage = default!;
    [SerializeField] private Image newPromotionImage = default!;


    private UIHelper.AttackRangeHelper attackRangeHelper = default!;

    [Header("Controls")]
    [SerializeField] private Button confirmButton = default!;

    [Header("UnlockContents")]
    //[SerializeField] private TextMeshProUGUI skillUnlockText = default!;
    //[SerializeField] private Image attackRangePrefab = default!;

    [SerializeField] private TextMeshProUGUI cannotConditionText = default!;

    private OwnedOperator op = default!;
    private string updateColor = string.Empty; // ��� ����.

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        updateColor = GameManagement.Instance!.ResourceManager.TextUpdateColor;
    }

    private void Start()
    {
        // ���� ���� �ð�ȭ ����� �ʱ�ȭ
        if (attackRangeContainer != null)
        {
            attackRangeHelper = UIHelper.Instance!.CreateAttackRangeHelper(
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
        if (attackRangeHelper == null) return;

        // ����ȭ ���� �⺻ ���� ����
        List<Vector2Int> baseTiles = new List<Vector2Int>(op.CurrentAttackableGridPos);

        // ����ȭ �� �߰��Ǵ� ���� ����
        List<Vector2Int>? additionalTiles = op.OperatorProgressData.elite1Unlocks.additionalAttackTiles;

        // �⺻ ������ �߰� ������ �ٸ� �������� ǥ��
        if (additionalTiles != null)
        {
            attackRangeHelper.ShowRangeWithUnlocks(baseTiles, additionalTiles);
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
