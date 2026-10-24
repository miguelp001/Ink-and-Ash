using System;

namespace InkAndAsh.Combat
{
    public class SkillAction
    {
        public string SkillId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int SkillCost { get; set; } = 100;
        public float Damage { get; set; }
        public float FocusCost { get; set; }
        public bool IsSyncMove { get; set; }
        public string TargetId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public SkillAction() { }

        public SkillAction(string id, string name, int cost, float damage = 0f, float focusCost = 0f, bool isSync = false)
        {
            SkillId = id ?? string.Empty;
            Name = name ?? string.Empty;
            SkillCost = cost;
            Damage = damage;
            FocusCost = focusCost;
            IsSyncMove = isSync;
        }
    }
}
