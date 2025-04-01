using System;
using System.Collections.Generic;

// ����ȭ�� �����ؼ� PlayerPrefs ������ �����ϰ� ��
[Serializable]
public class UserInventoryData
{
    [Serializable]
    public class ItemStack
    {
        public string itemName;
        public int count;


        public ItemStack(string itemName, int count)
        {
            this.itemName = itemName;
            this.count = count;
        }
    }

    public List<ItemStack> items = new List<ItemStack>();
}