using UnityEngine;

namespace Skills.Base
{
    [CreateAssetMenu(fileName = "New SixBullet Skill", menuName = "Skills/SixBullet Skill")]
    public class SixBulletSkill : AmmoBasedActiveSkill
    {
        protected override void SetDefaults()
        {
            autoRecover = true; 
        }
    }
}
