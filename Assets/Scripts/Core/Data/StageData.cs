using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Stage Data", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage Description")]
    public string stageId; // 1-1, 1-2, 1-3 ���
    public string stageName; // �������� ����
    public string stageDetail; // �������� ����

    [Header("Scene Settings")]
    public string sceneToLoad = "StageScene";

    [Header("Map Settings")]
    public string mapId;
    public GameObject mapPrefab; 

    [Header("Stage Functional Elements")]
    public float playerHealthMultiplier = 1f;
    public float enemyStatMultiplier = 1f;

}