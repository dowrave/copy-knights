using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Stage Data", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage Description")]
    public string stageId = string.Empty; // 1-1, 1-2, 1-3 ���
    public string stageName = string.Empty; // �������� ����
    [TextArea(3, 10)]
    public string stageDetail = string.Empty; // �������� ����

    [Header("Map Settings")]
    public GameObject mapPrefab = default!;

    [Header("Stage Configs")]
    public int startDeploymentCost = 20;
    public int maxDeploymentCost = 99;
    public float costPerSecondModifier = 1f; // �ڽ�Ʈ ȸ���ӵ� ����

    [Tooltip("�ִ�� ��ġ�� �� �ִ� ���۷������� ��")]
    public int operatorMaxDeploymentCount = 5; // ���۷����� �ִ� ��ġ ��

    [Header("Stage Functional Elements")]
    public float playerHealthMultiplier = 1f;
    public float enemyStatMultiplier = 1f;

    [Header("Deployable Settings")]
    [Tooltip("�� �ʿ����� ����� �� �ִ� ��ҵ�")]
    public List<MapDeployableData> mapDeployables = new List<MapDeployableData>();

    // 3�� Ŭ���� ���� ���޵Ǵ� ������. 
    [Header("Item List")]
    public List<ItemWithCount> FirstClearRewardItems = new List<ItemWithCount>(); // ���� Ŭ���� ����
    public List<ItemWithCount> BasicClearRewardItems = new List<ItemWithCount>(); // �׳� Ŭ�����ϸ� �ش�

}

[System.Serializable]
public struct ItemWithCount
{
    public ItemData itemData;
    public int count;

    public ItemWithCount(ItemData item, int c)
    {
        itemData = item;
        count = c;
    }
}