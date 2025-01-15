
using System.Collections.Generic;
using UnityEngine;

namespace Skills.Base
{
    [CreateAssetMenu(fileName = "New Meteor Skill", menuName = "Skills/Meteor Skill")]
    public class MeteorSkill : ActiveSkill
    {
        [Header("Skill Settings")]
        [SerializeField] private float damageMultiplier = 0.5f;
        [SerializeField] private float stunDuration = 2f;
        [SerializeField] private int costRecovery = 10;
        [SerializeField] private GameObject meteorEffectPrefab;

        private readonly List<Vector2Int> crossPattern = new List<Vector2Int>
        {
            new Vector2Int(-2, 0), new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, 2),
            new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(0, -2), new Vector2Int(1, 1),
            new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(2, 0)
        };

        protected override void SetDefaults()
        {
            autoRecover = true;
            autoActivate = false;
            modifiesAttackAction = false;
        }

        protected override void PlaySkillEffect(Operator op)
        {

        }
    }
}

