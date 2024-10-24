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
            DontDestroyOnLoad(gameObject); // ���� �ٲ� �ı����� ����
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

    /// ���߿� �� ������ ���۷����� ����Ʈ�� ���� �ִٰ� �������� ������ DeployableManager�� �������ָ� ��
}
