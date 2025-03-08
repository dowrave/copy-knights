using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Stage Data", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage Description")]
    public string stageId = string.Empty; // 1-1, 1-2, 1-3 등등
    public string stageName = string.Empty; // 스테이지 부제
    public string stageDetail = string.Empty; // 스테이지 설명

    [Header("Map Settings")]
    public GameObject mapPrefab = default!;

    [Header("Stage Configs")]
    public int startDeploymentCost = 10;
    public int maxDeploymentCost = 99;
    public float timeToFillCost = 1f; // 코스트 1을 올리는 데에 걸리는 시간

    [Tooltip("최대로 배치할 수 있는 오퍼레이터의 수")]
    public int operatorMaxDeploymentCount = 5; // 오퍼레이터 최대 배치 수

    [Header("Stage Functional Elements")]
    public float playerHealthMultiplier = 1f;
    public float enemyStatMultiplier = 1f;

    [Header("Deployable Settings")]
    [Tooltip("이 맵에서만 사용할 수 있는 요소들")]
    public List<MapDeployableData> mapDeployables = new List<MapDeployableData>();

    [Header("Item List")]
    [Tooltip("스테이지 클리어 시에 지급되는 아이템들")]
    public List<ItemWithCount> rewardItems = new List<ItemWithCount>(); // List<Dictionary<ItemData, int>>로 쓰는 것보다 별도의 클래스 or 구조체를 정의해서 사용하는 것이 좋은 방법
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