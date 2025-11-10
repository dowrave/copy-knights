using System;
using System.Collections.Generic;

// 직렬화로 구현해서 PlayerPrefs 저장을 용이하게 함
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