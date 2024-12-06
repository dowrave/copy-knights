using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item Data")]
public class ItemData : MonoBehaviour
{
    // 아이템 종류 : 경험치 소/중, 정예화 재료
    public enum ItemType
    {
        ExpSmall,
        ExpLarge,
        EliteItem
    }

    [Header("Item Identity")]
    public ItemType type;
    public string itemName;
    [TextArea(3, 10)]
    public string description; // 설명
    public Sprite icon;

    [Header("Item Effects")]
    public int expAmount; // 경험치 아이템일 때 제공하는 경험치량
    public bool canPromote; // 정예화 가능 여부

    // 아이템 효과 오퍼레이터에게 적용하기
    public bool UseOn(OwnedOperator target)
    {
        switch (type)
        {
            case ItemType.ExpSmall:
            case ItemType.ExpLarge:
                if (target.currentLevel >= OperatorGrowthSystem.GetMaxLevel(target.currentPhase))
                    return false;

                // 현재 페이즈의 최대 레벨을 고려한 경험치 적용
                target.currentExp = OperatorGrowthSystem.GetSafeExpAmount(
                    target.currentExp,
                    expAmount,
                    target.currentLevel,
                    target.currentPhase
                );
                return true; 

            case ItemType.EliteItem:
                if (!target.CanPromote)
                    return false;

                target.Promote();
                return true;

            default:
                return false;
        }
    }

}
