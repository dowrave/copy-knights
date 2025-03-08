using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Stage Data", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage Description")]
    public string stageId = string.Empty; // 1-1, 1-2, 1-3 ���
    public string stageName = string.Empty; // �������� ����
    public string stageDetail = string.Empty; // �������� ����

    [Header("Map Settings")]
    public GameObject mapPrefab = default!;

    [Header("Stage Configs")]
    public int startDeploymentCost = 10;
    public int maxDeploymentCost = 99;
    public float timeToFillCost = 1f; // �ڽ�Ʈ 1�� �ø��� ���� �ɸ��� �ð�

    [Tooltip("�ִ�� ��ġ�� �� �ִ� ���۷������� ��")]
    public int operatorMaxDeploymentCount = 5; // ���۷����� �ִ� ��ġ ��

    [Header("Stage Functional Elements")]
    public float playerHealthMultiplier = 1f;
    public float enemyStatMultiplier = 1f;

    [Header("Deployable Settings")]
    [Tooltip("�� �ʿ����� ����� �� �ִ� ��ҵ�")]
    public List<MapDeployableData> mapDeployables = new List<MapDeployableData>();

    [Header("Item List")]
    [Tooltip("�������� Ŭ���� �ÿ� ���޵Ǵ� �����۵�")]
    public List<ItemWithCount> rewardItems = new List<ItemWithCount>(); // List<Dictionary<ItemData, int>>�� ���� �ͺ��� ������ Ŭ���� or ����ü�� �����ؼ� ����ϴ� ���� ���� ���
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