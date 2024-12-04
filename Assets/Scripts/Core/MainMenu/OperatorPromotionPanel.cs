using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OperatorPromotionPanel : MonoBehaviour
{

    [Header("Controls")]
    [SerializeField] private Button confirmButton;

    [Header("UnlockContents")]
    [SerializeField] private TextMeshProUGUI SkillUnlockText;
    [SerializeField] private Image attackRangePrefab;

    private OwnedOperator op;
    private string updateColor;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        updateColor = GameManagement.Instance.ResourceManager.textUpdateColor;
    }

    public void Initialize(OwnedOperator op)
    {
        this.op = op;
        UpdateUI();
    }

    private void UpdateUI()
    {
        bool canPromote = op.CanPromote;
        confirmButton.interactable = canPromote;
    }

    private void OnConfirmButtonClicked()
    {
        if (op.CanPromote)
        {
            OperatorGrowthManager.Instance.TryPromoteOperator(op);
        }
    }

    private void ShowAttackRangePreview()
    {
        ClearRangeVisuals();

        //CreateRangeTile(Vector2Int.zero, true, false);
    }

    private void ClearRangeVisuals()
    {

    }
}
