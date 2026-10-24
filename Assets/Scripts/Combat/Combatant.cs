using System;

namespace InkAndAsh.Combat
{
    public class Combatant
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsAlly { get; set; }
        public float BaseAgility { get; set; } = 50f;
        public float CurrentHealth { get; set; } = 100f;
        public float MaxHealth { get; set; } = 100f;
        public float FocusPoints { get; set; } = 50f;
        public float NextTurnTime { get; set; } = 0f;
        public bool HasInjuryDebuff { get; set; }
        public string LinkedAllyId { get; set; }

        public bool IsAlive => CurrentHealth > 0f;
        public float EffectiveAgility => HasInjuryDebuff ? Math.Max(1.0f, BaseAgility * 0.8f) : Math.Max(1.0f, BaseAgility);

        public Combatant Clone()
        {
            return new Combatant
            {
                Id = Id,
                Name = Name,
                IsAlly = IsAlly,
                BaseAgility = BaseAgility,
                CurrentHealth = CurrentHealth,
                MaxHealth = MaxHealth,
                FocusPoints = FocusPoints,
                NextTurnTime = NextTurnTime,
                HasInjuryDebuff = HasInjuryDebuff,
                LinkedAllyId = LinkedAllyId
            };
        }
    }
}
