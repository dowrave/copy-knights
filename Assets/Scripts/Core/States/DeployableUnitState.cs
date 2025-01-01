using static DeployableManager;
using UnityEngine;

/// <summary>
/// 배치 가능한 유닛의 현재 세션에서의 상태를 관리합니다.
/// </summary>
public class DeployableUnitState
{
    private readonly DeployableInfo deployableInfo;
    public int DeploymentCount { get; private set; }
    public int CurrentDeploymentCost { get; private set; }
    public int RemainingDeployCount { get; private set; }
    public bool IsOnCooldown { get; private set; }
    public float CooldownTimer { get; private set; }

    private const int MAX_COST_INCREASE_COUNT = 2;
    private const float COST_INCREASE_RATE = 0.5f;

    public bool IsOperator { get; private set; }

    public DeployableUnitState(DeployableInfo info)
    {
        deployableInfo = info;
        IsOperator = deployableInfo.ownedOperator != null;

        CurrentDeploymentCost = IsOperator ? 
                info.ownedOperator.BaseData.stats.DeploymentCost : 
                info.deployableUnitData.stats.DeploymentCost;

        RemainingDeployCount = info.maxDeployCount;
        IsOnCooldown = false;
        CooldownTimer = 0f;
    }

    public bool OnDeploy()
    {
        DeploymentCount++;
        RemainingDeployCount--;
        
        if (!IsOperator)
        {
            StartCooldown(deployableInfo.redeployTime);
        }

        return true;
    }


    /// <summary>
    /// 배치된 유닛이 제거될 때 호출되는 메서드
    /// </summary>
    public void OnRemoved()
    {
        if (IsOperator)
        {
            RemainingDeployCount++;
            StartCooldown(deployableInfo.redeployTime);
            UpdateDeploymentCost();
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

    private void UpdateDeploymentCost()
    {
        if (deployableInfo.ownedOperator == null) return;

        int baseCost = deployableInfo.ownedOperator.BaseData.stats.DeploymentCost;
        float multiplier = 1f + (Mathf.Min(DeploymentCount - 1, MAX_COST_INCREASE_COUNT - 1) * COST_INCREASE_RATE);
        CurrentDeploymentCost = Mathf.RoundToInt(baseCost * multiplier);
    }
}