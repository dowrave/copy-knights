using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public List<EnemySpawnInfo> spawnList = new List<EnemySpawnInfo>(); // �����Ǵ� ��, �ð��� ���� ����
    private bool isInitialized = false;

    public void Initialize()
    {
        isInitialized = true;
    }


    public void StartSpawning()
    {
        if (isInitialized)
        {
            StartCoroutine(SpawnEntities());
        }
    }

    private IEnumerator SpawnEntities()
    {
        if (spawnList.Count == 0)
        {
            yield break;
        }
        float startTime = Time.time;


        foreach (var spawnInfo in spawnList)
        {
            yield return new WaitForSeconds(spawnInfo.spawnTime - (Time.time - startTime));

            Spawn(spawnInfo);
        }
    }

    private void Spawn(EnemySpawnInfo spawnInfo)
    {
        if (spawnInfo.prefab == null) return;

        GameObject spawnedObject = Instantiate(spawnInfo.prefab, transform.position, Quaternion.identity);

        if (spawnedObject.TryGetComponent(out Enemy enemy))
        {
            EnemyData enemyData = enemy.BaseData;
            enemy.Initialize(enemyData, spawnInfo.pathData);
        }
        else if (spawnedObject.TryGetComponent(out PathIndicator pathIndicator))
        {
            pathIndicator.Initialize(spawnInfo.pathData);
        }
        else
        {
            Debug.LogError("�����տ� Enemy�� PathIndicator ������Ʈ�� ����");
            Destroy(gameObject);
        }
    }
}