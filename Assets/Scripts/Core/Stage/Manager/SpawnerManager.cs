using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    public static SpawnerManager Instance { get; private set; }
    private List<EnemySpawner> spawners = new List<EnemySpawner>();
    private Map currentMap;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(Map map)
    {
        currentMap = map;
        FindAllSpawners();
    }

    /// <summary>
    /// 맵에 있는 스포너들을 찾아서 자동으로 등록함
    /// </summary>
    private void FindAllSpawners()
    {
        spawners.Clear();

        // Map 찾기
        Map currentMap = MapManager.Instance?.CurrentMap; 
        if (currentMap != null)
        {
            // FindObjectsOfType이나 GetComponentsInChildren이나 모두 리스트를 찾음
            EnemySpawner[] foundSpawners = currentMap.GetComponentsInChildren<EnemySpawner>();
            foreach (EnemySpawner spawner in foundSpawners)
            {
                spawners.Add(spawner);
                spawner.Initialize();
            }
        }

    }


    // 스테이지 매니저에서 실행
    public void StartSpawning()
    {
        foreach (EnemySpawner spawner in spawners)
        {
            if (spawner != null)
            {
                spawner.StartSpawning();
            }
            else
            {
                Debug.LogWarning("Null EnemySpawner found in the list.");
            }
        }
    }

}
