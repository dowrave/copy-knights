using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Stage Data", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage Description")]
    public string stageId; // 1-1, 1-2, 1-3 등등
    public string stageName; // 스테이지 부제
    public string stageDetail; // 스테이지 설명

    [Header("Map Settings")]
    public string mapId;
    public GameObject mapPrefab;

    [Header("Stage Configs")]
    public int startDeploymentCost = 10;
    public int maxDeploymentCost = 99;
    public float timeToFillCost = 1f; // 코스트 1을 올리는 데에 걸리는 시간

    [Header("Stage Functional Elements")]
    public float playerHealthMultiplier = 1f;
    public float enemyStatMultiplier = 1f;

}