using UnityEngine;

[System.Serializable]
public class EnemySpawnInfo
{
    public EnemyType enemyType;
    public GameObject enemyPrefab;
    public float spawnTime;
    public PathData pathData; // StageData������ ��� �����͸� ���� �����ϵ��� ����
}

public enum EnemyType
{
    Regular,
    Elite,
    Boss
}