using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupManager : MonoBehaviour
{
    public static PopupManager? Instance { get; private set; }

    [Header("Popup Prefabs")]
    [SerializeField] private MainmenuItemInfoPopup? itemInfoPopup;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (itemInfoPopup != null)
        {
            itemInfoPopup.gameObject.SetActive(false);
        }
    }

    public void ShowItemInfoPopup(ItemData itemData)
    {
        if (itemInfoPopup != null)
        {
            itemInfoPopup.gameObject.SetActive(true);
            itemInfoPopup.Initialize(itemData);
        }
    }

    public void OnPopupClosed(MainmenuItemInfoPopup popup)
    {
        if (itemInfoPopup != null)
        {
            itemInfoPopup.gameObject.SetActive(false);
            itemInfoPopup.ClearData(); // 데이터 초기화
        }
    }
}
