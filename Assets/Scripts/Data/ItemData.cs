using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    // ������ ���� : ����ġ ��/��, ����ȭ ���
    public enum ItemType
    {
        Exp,
        EliteItem
    }

    [Header("Item Identity")]
    public ItemType type;
    public string itemName = string.Empty; // �ʱ�ȭ
    [TextArea(3, 10)]
    public string description = string.Empty; // ����
    public Sprite icon = default!;
 
    [Header("Item Effects")]
    public int expAmount = 0; // ����ġ �������� �� �����ϴ� ����ġ��
    public bool canPromote = false; // ����ȭ ���� ����

    // ������ ȿ�� ���۷����Ϳ��� �����ϱ�
    public bool UseOn(OwnedOperator target)
    {
        switch (type)
        {
            case ItemType.Exp:
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
