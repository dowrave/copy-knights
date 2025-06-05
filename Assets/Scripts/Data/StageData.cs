using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Stage Data", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage Description")]
    public string stageId = string.Empty; // 1-1, 1-2, 1-3 등등
    public string stageName = string.Empty; // 스테이지 부제
    [TextArea(3, 10)]
    public string stageDetail = string.Empty; // 스테이지 설명

    [Header("Map Settings")]
    public GameObject mapPrefab = default!;

    [Header("Stage Configs")]
    public int startDeploymentCost = 20;
    public int maxDeploymentCost = 99;
    public float costPerSecondModifier = 1f; // 코스트 회복속도 배율

    [Tooltip("최대로 배치할 수 있는 오퍼레이터의 수")]
    public int operatorMaxDeploymentCount = 5; // 오퍼레이터 최대 배치 수

    [Header("Stage Functional Elements")]
    public float playerHealthMultiplier = 1f;
    public float enemyStatMultiplier = 1f;

    [Header("Deployable Settings")]
    [Tooltip("이 맵에서만 사용할 수 있는 요소들")]
    public List<MapDeployableData> mapDeployables = new List<MapDeployableData>();

    // 3성 클리어 기준 지급되는 아이템. 
    [Header("Item List")]
    public List<ItemWithCount> FirstClearRewardItems = new List<ItemWithCount>(); // 최초 클리어 기준
    public List<ItemWithCount> BasicClearRewardItems = new List<ItemWithCount>(); // 그냥 클리어하면 준다

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