using static DeployableManager;
using UnityEngine;


// ��ġ ������ ������ ���� ���ǿ����� ���¸� �����մϴ�.
public class DeployableUnitState
{
    private readonly DeployableInfo deployableInfo;
    public int DeploymentCount { get; private set; } // �� ��ġ�� Ƚ��, 0���� ����
    public int CurrentDeploymentCost { get; private set; }
    public int RemainingDeployCount { get; private set; } // ���� ��ġ ��, ���۷����Ͷ�� 1
    public bool IsOnCooldown { get; private set; }
    public float CooldownTimer { get; private set; }
    public bool IsDeployed { get; private set; } // ���۷������� ��쿡�� ����

    public DeployableUnitEntity? currentDeployable;
    public Operator? currentOperator;

    private const float COST_INCREASE_RATE = 0.5f;

    public bool IsOperator { get; private set; }

    public DeployableUnitState(DeployableInfo info)
    {
        deployableInfo = info;
        IsOperator = deployableInfo.ownedOperator != null;

        CurrentDeploymentCost = IsOperator && info.ownedOperator != null ?
            info.ownedOperator.OperatorProgressData.stats.DeploymentCost :
            info.deployableUnitData?.stats.DeploymentCost ?? 0;

        RemainingDeployCount = info.maxDeployCount;
        IsOnCooldown = false;
        IsDeployed = false;
        CooldownTimer = 0f;
    }

    public bool OnDeploy(DeployableUnitEntity deployableUnitEntity)
    {
        DeploymentCount++;
        RemainingDeployCount--;

        // ��ġ�� ������ ����
        if (deployableUnitEntity is Operator op)
        {
            currentOperator = op;
            IsDeployed = true;
        }
        else
        {
            currentDeployable = deployableUnitEntity;
        }

        if (!IsOperator)
        {
            StartCooldown(deployableInfo.redeployTime);
        }

        return true;
    }

    // ��ġ�� ������ ���ŵ� �� ȣ��Ǵ� �޼���
    public void OnRemoved()
    {
        if (IsOperator)
        {
            RemainingDeployCount++;
            StartCooldown(deployableInfo.redeployTime);
            UpdateDeploymentCost();
            IsDeployed = false;
        }
    }

    public void StartCooldown(float duration)
    {
        IsOnCooldown = true;
        CooldownTimer = duration;
    }

    public void UpdateCooldown()
    {
        if (!IsOnCooldown) return;

        CooldownTimer -= Time.deltaTime;
        if (CooldownTimer <= 0)
        {
            IsOnCooldown = false;
            CooldownTimer = 0f;
        }
    }

    // ���۷������� ��쿡�� ����Ǵ� ��ġ �ڽ�Ʈ ����
    private void UpdateDeploymentCost()
    {
        if (deployableInfo.ownedOperator == null) return;

        // �⺻ �ڽ�Ʈ
        int baseCost = deployableInfo.ownedOperator.OperatorProgressData.stats.DeploymentCost;

        // 1, 1.5, 2�� ���� �� �ִ�.
        // ��) ���ġ���� 50%�� ��ġ �ڽ�Ʈ�� ����, �ִ� 2ȸ(�� 2ȸ ���ĺ��ʹ� ������ 2�� �ڽ�Ʈ�� ��)������.
        float multiplier = 1f + (Mathf.Min(DeploymentCount, 2) * COST_INCREASE_RATE);

        // ���� ��ġ ��� ����
        CurrentDeploymentCost = Mathf.RoundToInt(baseCost * multiplier);
    }
}