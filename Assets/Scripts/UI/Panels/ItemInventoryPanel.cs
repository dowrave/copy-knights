using System.Collections.Generic;
using UnityEngine;

public class ItemInventoryPanel : MonoBehaviour
{
    [SerializeField] private Transform itemContainer = default!; // gridLayOutGroup
    [SerializeField] private ItemUIElement itemUIPrefab = default!;

    private List<ItemUIElement> itemElements = new List<ItemUIElement>();

    private void Start()
    {
        InitializeInventory();   
    }

    private void InitializeInventory()
    {
        var items = GameManagement.Instance!.PlayerDataManager.GetAllItems();

        foreach (var (itemData, count) in items)
        {
            CreateItemElement(itemData, count);
        }
    }

    private void CreateItemElement(ItemData itemData, int count)
    {
        ItemUIElement element = Instantiate(itemUIPrefab, itemContainer);
        element.Initialize(itemData, count, false);
        itemElements.Add(element);
    }

    // 아이템 갯수 변경시 UI 업데이트
    private void UpdateItemCount(ItemData itemData, int count)
    {
        ItemUIElement element = Instantiate(itemUIPrefab, itemContainer);
        element.Initialize(itemData, count, false);
        itemElements.Add(element);
    }
}