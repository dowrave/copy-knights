using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item Data")]
public class ItemData : MonoBehaviour
{
    // ������ ���� : ����ġ ��/��, ����ȭ ���
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
    public string description; // ����
    public Sprite icon;

    [Header("Item Effects")]
    public int expAmount; // ����ġ �������� �� �����ϴ� ����ġ��
    public bool canPromote; // ����ȭ ���� ����

    // ������ ȿ�� ���۷����Ϳ��� �����ϱ�
    public bool UseOn(OwnedOperator target)
    {
        switch (type)
        {
            case ItemType.ExpSmall:
            case ItemType.ExpLarge:
                if (target.currentLevel >= OperatorGrowthSystem.GetMaxLevel(target.currentPhase))
                    return false;

                // ���� �������� �ִ� ������ ����� ����ġ ����
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
