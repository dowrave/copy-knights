using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UserSquadManager : MonoBehaviour
{
    public static UserSquadManager Instance { get; private set; }

    [System.Serializable]
    public class SquadMember
    {
        public GameObject operatorPrefab;
    }

    [SerializeField] private List<SquadMember> userSquad = new List<SquadMember>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateUserSquad(List<GameObject> newSquad)
    {
        userSquad.Clear();
        foreach (var operatorPrefab in newSquad)
        {
            userSquad.Add(new SquadMember { operatorPrefab = operatorPrefab });
        }
    }

    public List<GameObject> GetUserSquad()
    {
        return userSquad.Select(member => member.operatorPrefab).ToList();
    }

    /// 나중에 편성 씬에서 오퍼레이터 리스트를 갖고 있다가 스테이지 씬에서 DeployableManager로 전달해주면 됨
}
