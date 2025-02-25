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
    public GameObject mapPrefab;

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
}