using UnityEngine;


public class DeployableUnitData : UnitData
{
    public new DeployableUnitStats stats;
    public bool canDeployOnGround = false;
    public bool canDeployOnHill = false;
    public Sprite icon; // UI ǥ�� ������
    public GameObject prefab; // ���۷������� ������ ����
}
