using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StageResultData
{
    public int passedEnemies;
    public bool isCleared;
    public List<StatisticsManager.OperatorStats> operatorStats;

    public int StarCount => Mathf.Max(0, 3 - passedEnemies);
}

