using UnityEngine;


public class DeployableUnitData : UnitData
{
    public int deploymentCost;
    public bool canDeployOnGround = false;
    public bool canDeployOnHill = false;
    public float redeployTime = 70f; // ���ġ ��� �ð�
    public Sprite icon; // UI ǥ�� ������
    public GameObject prefab; // ���۷������� ������ ����
}
