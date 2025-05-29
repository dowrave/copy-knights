using static DeployableManager;
using UnityEngine;


// 배치 가능한 유닛의 현재 세션에서의 상태를 관리합니다.
public class DeployableUnitState
{
    private readonly DeployableInfo deployableInfo;
    public int DeploymentCount { get; private set; } // 총 배치된 횟수, 0에서 시작
    public int CurrentDeploymentCost { get; private set; }
    public int RemainingDeployCount { get; private set; } // 남은 배치 수, 오퍼레이터라면 1
    public bool IsOnCooldown { get; private set; }
    public float CooldownTimer { get; private set; }
    public bool IsDeployed { get; private set; } // 오퍼레이터인 경우에만 추적

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

        // 배치된 유닛을 추적
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

    // 배치된 유닛이 제거될 때 호출되는 메서드
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

    // 오퍼레이터일 경우에만 수행되는 배치 코스트 갱신
    private void UpdateDeploymentCost()
    {
        if (deployableInfo.ownedOperator == null) return;

        // 기본 코스트
        int baseCost = deployableInfo.ownedOperator.OperatorProgressData.stats.DeploymentCost;

        // 1, 1.5, 2만 가질 수 있다.
        // 상세) 재배치마다 50%식 배치 코스트가 증가, 최대 2회(즉 2회 이후부터는 최초의 2배 코스트가 듦)까지만.
        float multiplier = 1f + (Mathf.Min(DeploymentCount, 2) * COST_INCREASE_RATE);

        // 현재 배치 비용 갱신
        CurrentDeploymentCost = Mathf.RoundToInt(baseCost * multiplier);
    }
}