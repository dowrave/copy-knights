using UnityEngine;

namespace Skills.Base
{
    public class BossRangedSkill : EnemyBossSkill
    {
        [SerializeField] ParticleSystem castVFX = default!; 

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