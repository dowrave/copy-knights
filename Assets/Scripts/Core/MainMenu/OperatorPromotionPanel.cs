using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OperatorPromotionPanel : MonoBehaviour
{

    [Header("Controls")]
    [SerializeField] private Button confirmButton; 

    private OwnedOperator op;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
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
}
