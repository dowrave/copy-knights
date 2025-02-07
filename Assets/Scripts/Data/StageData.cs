using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Stage Data", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage Description")]
    public string stageId; // 1-1, 1-2, 1-3 ���
    public string stageName; // �������� ����
    public string stageDetail; // �������� ����

    [Header("Map Settings")]
    public string mapId;
    public GameObject mapPrefab;

    [Header("Stage Configs")]
    public int startDeploymentCost = 10;
    public int maxDeploymentCost = 99;
    public float timeToFillCost = 1f; // �ڽ�Ʈ 1�� �ø��� ���� �ɸ��� �ð�

    [Header("Stage Functional Elements")]
    public float playerHealthMultiplier = 1f;
    public float enemyStatMultiplier = 1f;

}