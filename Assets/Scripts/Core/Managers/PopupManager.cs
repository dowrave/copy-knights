using System;
using UnityEngine;

// 메인메뉴 씬에서 클릭 가능한 오브젝트를 클릭했을 때 나타나는 패널 / 팝업 등을 관리합니다.
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

    // 리턴을 주는 이유는 확인 버튼에 이벤트를 추가하기 위함
    public ConfirmationPopup ShowConfirmationPopup(string text, bool isCancelButton, bool blurAreaActivation, Action onConfirm = null, Action onCancel = null)
    {
        // 없을 때에만 최초에 한 번 생성
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
            itemInfoPopup.ClearData(); // 데이터 초기화
        }
    }
}
