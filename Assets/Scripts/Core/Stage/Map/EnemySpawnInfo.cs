using UnityEngine;

[System.Serializable]
public class EnemySpawnInfo
{
    public GameObject enemyPrefab;
    public float spawnTime;
    public PathData pathData; // StageData������ ��� �����͸� ���� �����ϵ��� ����
}
