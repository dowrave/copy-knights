using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    public List<EnemySpawner> spawners = new List<EnemySpawner>();
    private MapManager mapManager; 

    public void Initialize(MapManager manager)
    {
        mapManager = manager;
        FindAllSpawners();
    }

    private void FindAllSpawners()
    {
        spawners.Clear();
        EnemySpawner[] foundSpawners = FindObjectsOfType<EnemySpawner>();
        foreach (EnemySpawner spawner in foundSpawners)
        {
            spawners.Add(spawner);
            spawner.Initialize(mapManager); // �� �����ʿ� mapManager ����
        }
    }

    public void StartSpawning()
    {
        foreach (EnemySpawner spawner in spawners)
        {
            spawner.StartSpawning();
        }
    }

}
