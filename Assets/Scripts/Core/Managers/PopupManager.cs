using System;
using UnityEngine;

// ���θ޴� ������ Ŭ�� ������ ������Ʈ�� Ŭ������ �� ��Ÿ���� �г� / �˾� ���� �����մϴ�.
public class PopupManager : MonoBehaviour
{
    public static PopupManager? Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private MainmenuItemInfoPopup? itemInfoPopup;
    [SerializeField] private ConfirmationPopup? confirmationPopupPrefab;

    [Header("Canvas")]
    [SerializeField] private Canvas canvas;

    private ConfirmationPopup confirmationPopupInstance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (itemInfoPopup != null)
        {
            itemInfoPopup.gameObject.SetActive(false);
        }
        canvas = transform.parent.GetComponentInChildren<Canvas>();

    }

    public void ShowItemInfoPopup(ItemData itemData)
    {
        if (itemInfoPopup != null)
        {
            itemInfoPopup.gameObject.SetActive(true);
            itemInfoPopup.Initialize(itemData);
        }
    }

    // ������ �ִ� ������ Ȯ�� ��ư�� �̺�Ʈ�� �߰��ϱ� ����
    public ConfirmationPopup ShowConfirmationPopup(string text, bool isCancelButton, bool blurAreaActivation, Action onConfirm = null, Action onCancel = null)
    {
        // ���� ������ ���ʿ� �� �� ����
        if (confirmationPopupInstance == null)
        {
            confirmationPopupInstance = Instantiate(confirmationPopupPrefab, canvas.transform);
        }

        confirmationPopupInstance.gameObject.SetActive(true); 
        confirmationPopupInstance.Initialize(text, isCancelButton, blurAreaActivation, onConfirm, onCancel);
        return confirmationPopupInstance;
    }

    public void OnItemInfoPopupClosed(MainmenuItemInfoPopup popup)
    {
        if (itemInfoPopup != null)
        {
            itemInfoPopup.gameObject.SetActive(false);
            itemInfoPopup.ClearData(); // ������ �ʱ�ȭ
        }
    }
}
