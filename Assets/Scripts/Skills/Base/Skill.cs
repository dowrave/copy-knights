using UnityEngine;

namespace Skills.Base
{
    public abstract class Skill: ScriptableObject
    {
        public string Name;
        [TextArea(3, 10)]
        public string description;
        public float SPCost;
        public Sprite SkillIcon;

        public abstract void Activate(Operator op);
    }

}