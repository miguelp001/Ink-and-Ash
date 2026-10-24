using System;
using System.Collections.Generic;
using System.Linq;

namespace InkAndAsh.Combat
{
    public class TimelineQueue
    {
        private readonly List<Combatant> _combatants = new List<Combatant>();

        public int Count => _combatants.Count;
        public IReadOnlyList<Combatant> Combatants => _combatants;

        public void Add(Combatant combatant)
        {
            if (combatant == null) return;
            _combatants.Add(combatant);
            Sort();
        }

        public bool Remove(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            int removed = _combatants.RemoveAll(c => c.Id == id);
            return removed > 0;
        }

        public void Clear()
        {
            _combatants.Clear();
        }

        public void Sort()
        {
            _combatants.Sort(CompareCombatants);
        }

        public Combatant Peek()
        {
            return _combatants.Count > 0 ? _combatants[0] : null;
        }

        public IReadOnlyList<Combatant> AsReadOnly()
        {
            return _combatants;
        }

        public List<Combatant> CloneList()
        {
            return _combatants.Select(c => c.Clone()).ToList();
        }

        public static int CompareCombatants(Combatant a, Combatant b)
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int timeComparison = a.NextTurnTime.CompareTo(b.NextTurnTime);
            if (timeComparison != 0) return timeComparison;

            // Allies take priority over enemies
            if (a.IsAlly != b.IsAlly)
            {
                return a.IsAlly ? -1 : 1;
            }

            // Higher agility acts earlier
            int agiComparison = b.EffectiveAgility.CompareTo(a.EffectiveAgility);
            if (agiComparison != 0) return agiComparison;

            // Deterministic stable string comparison
            return string.Compare(a.Id, b.Id, StringComparison.Ordinal);
        }
    }
}
