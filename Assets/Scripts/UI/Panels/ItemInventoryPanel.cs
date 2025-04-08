using System.Collections.Generic;
using UnityEngine;

public class ItemInventoryPanel : MonoBehaviour
{
    [SerializeField] private Transform itemContainer = default!; // gridLayOutGroup
    [SerializeField] private ItemUIElement itemUIPrefab = default!;

    private List<ItemUIElement> itemElements = new List<ItemUIElement>();

    private void OnEnable()
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

    private void ClearOldElements()
    {
        // ������ ��� UI ������Ʈ�� �ı� �� ����Ʈ �ʱ�ȭ
        foreach (var element in itemElements)
        {
            Destroy(element.gameObject);
        }
        itemElements.Clear();
    }

    private void OnDisable()
    {
        ClearOldElements();
    }
}