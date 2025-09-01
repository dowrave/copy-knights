using UnityEngine;
using System.Collections.Generic;

public class EnemyBoss : Enemy
{
    [SerializeField] EnemyBossData bossData = default!;

    public override EnemyData BaseData => bossData;
    public EnemyBossData BossData => bossData;

    // bossData에서 가져와서 관리할 계획
    private List<EnemyBossSkill> skills;
    private Dictionary<EnemyBossSkill, float> skillCooldowns = new Dictionary<EnemyBossSkill, float>();

    public override void SetPrefab()
    {
        prefab = bossData.prefab;
    }

    protected override void DecideAndPerformAction()
    {
        if (TryUseSkill())
        {
            return;
        }

        base.DecideAndPerformAction();
    }

    private bool TryUseSkill()
    {
        // 1. 저지 당한 상태 & 근거리 스킬 쿨타임이 아닐 때 근거리 스킬을 사용함

        // 2. 저지 당하지 않았음 & 원거리 스킬 쿨타임이 아닐 때 & 범위 내에 Operator가 있을 때 원거리 스킬을 사용함
        
        
        return false;
    }
}