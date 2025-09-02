using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Boss Data", menuName = "Game/Boss Data")]
public class EnemyBossData : EnemyData
{
    [Header("Boss Properties")]
    [Tooltip("저지 당했을 때 사용할 근거리 스킬들")]
    [SerializeField] private List<EnemyBossSkill> meleeSkills = new List<EnemyBossSkill>();
    [Tooltip("저지 당하지 않은 상태에서 오퍼레이터를 대상으로 사용하는 스킬들")]
    [SerializeField] private List<EnemyBossSkill> rangedSkills = new List<EnemyBossSkill>();

    // 프로퍼티
    public List<EnemyBossSkill> MeleeSkills => meleeSkills;
    public List<EnemyBossSkill> RangedSkills => rangedSkills;
}
