using UnityEngine;

namespace Skills.Base
{
    [CreateAssetMenu(fileName = "New Boss Ranged Skill", menuName = "Skills/Boss/Ranged Skill")]
    public class BossRangedSkill : EnemyBossSkill
    {
        [SerializeField] GameObject castVFXPrefab = default!;

        ParticleSystem castVFX;

        private void Awake()
        {
            if (castVFXPrefab != null)
            {
                castVFX = castVFXPrefab.GetComponent<ParticleSystem>();
            }
        }

        public override void Activate(EnemyBoss boss)
        {
            PlayCastVFX();
        }

        // Boss가 스킬을 시전할 때, Boss에서 나타나는 스킬 시전 이펙트
        protected void PlayCastVFX()
        {
            castVFX.Play(true);
        }
    }
}