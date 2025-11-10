using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    // 아이템 종류 : 경험치 소/중, 정예화 재료
    public enum ItemType
    {
        Exp,
        EliteItem
    }

    [Header("Item Identity")]
    public ItemType type;
    public string itemName = string.Empty; // 초기화
    [TextArea(3, 10)]
    public string description = string.Empty; // 설명
    public Sprite icon = default!;
 
    [Header("Item Effects")]
    public int expAmount = 0; // 경험치 아이템일 때 제공하는 경험치량
    public bool canPromote = false; // 정예화 가능 여부

    // 아이템 효과 오퍼레이터에게 적용하기
    public bool UseOn(OwnedOperator target)
    {
        switch (type)
        {
            case ItemType.Exp:
                if (target.CurrentLevel >= OperatorGrowthSystem.GetMaxLevel(target.CurrentPhase))
                    return false;

                // 현재 페이즈의 최대 레벨을 고려한 경험치 적용
                target.SetCurrentExp(OperatorGrowthSystem.GetSafeExpAmount(
                    target.CurrentExp,
                    expAmount,
                    target.CurrentLevel,
                    target.CurrentPhase
                ));
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
