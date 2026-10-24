using System;
using System.Collections.Generic;

namespace InkAndAsh.Combat
{
#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
    public class CombatController : UnityEngine.MonoBehaviour
#else
    public class CombatController
#endif
    {
        public CtbCombatEngine Engine { get; } = new CtbCombatEngine();

        public event Action<Combatant> OnTurnReady;
        public event Action OnBattleWon;
        public event Action OnBattleLost;

        public void InitializeBattle(IEnumerable<Combatant> combatants)
        {
            Engine.Clear();
            if (combatants != null)
            {
                foreach (var c in combatants)
                {
                    Engine.AddCombatant(c);
                }
            }
        }

        public Combatant GetCurrentActor()
        {
            if (Engine.Timeline.Count == 0) return null;
            return Engine.Timeline[0];
        }

        public void ExecuteAction(Combatant actor, int skillCost, float weatherMultiplier = 1.0f)
        {
            if (actor == null) return;
            Engine.CommitAction(actor, skillCost, weatherMultiplier);

            CheckBattleState();

            var nextActor = GetCurrentActor();
            if (nextActor != null)
            {
                OnTurnReady?.Invoke(nextActor);
            }
        }

        public void CheckBattleState()
        {
            bool anyAlliesAlive = false;
            bool anyEnemiesAlive = false;

            foreach (var c in Engine.Timeline)
            {
                if (c.IsAlive)
                {
                    if (c.IsAlly) anyAlliesAlive = true;
                    else anyEnemiesAlive = true;
                }
            }

            if (!anyEnemiesAlive && anyAlliesAlive)
            {
                OnBattleWon?.Invoke();
            }
            else if (!anyAlliesAlive && anyEnemiesAlive)
            {
                OnBattleLost?.Invoke();
            }
        }
    }
}
