using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Boss Data", menuName = "Game/Boss Data")]
public class EnemyBossData : EnemyData
{
    [Header("Boss Properties")]
    [Tooltip("이 보스가 사용하는 스킬 목록입니다.")]
    [SerializeField]
    private List<EnemyBossSkill> skills = new List<EnemyBossSkill>();
    public List<EnemyBossSkill> Skills => skills;

}
