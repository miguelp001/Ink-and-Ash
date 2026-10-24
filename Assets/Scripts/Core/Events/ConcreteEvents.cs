using System;
using System.Collections.Generic;

namespace InkAndAsh.Core.Events
{
    // Concrete Event Payload Data Types
    [Serializable]
    public struct CombatStartData
    {
        public List<string> partyMemberIds;
        public List<string> enemyIds;
        public string biomeId;

        public CombatStartData(List<string> party, List<string> enemies, string biome)
        {
            partyMemberIds = party ?? new List<string>();
            enemyIds = enemies ?? new List<string>();
            biomeId = biome ?? string.Empty;
        }
    }

    [Serializable]
    public struct CombatEndData
    {
        public string result; // "Victory", "Defeat", "PeacefulResolution"
        public int turnsElapsed;

        public CombatEndData(string combatResult, int turns)
        {
            result = combatResult ?? string.Empty;
            turnsElapsed = turns;
        }
    }

    [Serializable]
    public struct TimeTickData
    {
        public int hour;
        public int day;

        public TimeTickData(int currentHour, int currentDay)
        {
            hour = currentHour;
            day = currentDay;
        }
    }

    [Serializable]
    public struct ItemDecayedData
    {
        public string itemId;
        public float currentDurability;
        public bool wasSpoiled;

        public ItemDecayedData(string id, float durability, bool spoiled)
        {
            itemId = id ?? string.Empty;
            currentDurability = durability;
            wasSpoiled = spoiled;
        }
    }

    [Serializable]
    public struct QuestActData
    {
        public string questId;
        public string actName;
        public int actNumber;

        public QuestActData(string qId, string act, int actNum)
        {
            questId = qId ?? string.Empty;
            actName = act ?? string.Empty;
            actNumber = actNum;
        }
    }

    [Serializable]
    public struct SpireCalibratedData
    {
        public string biomeId;
        public string spireName;

        public SpireCalibratedData(string bId, string sName)
        {
            biomeId = bId ?? string.Empty;
            spireName = sName ?? string.Empty;
        }
    }

    // Concrete ScriptableObject Event Channels
#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
    [UnityEngine.CreateAssetMenu(fileName = "OnCombatStartedEvent", menuName = "InkAndAsh/Events/OnCombatStarted")]
#endif
    public class OnCombatStartedEvent : GameEvent<CombatStartData> { }

#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
    [UnityEngine.CreateAssetMenu(fileName = "OnCombatEndedEvent", menuName = "InkAndAsh/Events/OnCombatEnded")]
#endif
    public class OnCombatEndedEvent : GameEvent<CombatEndData> { }

#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
    [UnityEngine.CreateAssetMenu(fileName = "OnTimeHourTickEvent", menuName = "InkAndAsh/Events/OnTimeHourTick")]
#endif
    public class OnTimeHourTickEvent : GameEvent<TimeTickData> { }

#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
    [UnityEngine.CreateAssetMenu(fileName = "OnTimeDayTickEvent", menuName = "InkAndAsh/Events/OnTimeDayTick")]
#endif
    public class OnTimeDayTickEvent : GameEvent<int> { }

#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
    [UnityEngine.CreateAssetMenu(fileName = "OnItemDecayedEvent", menuName = "InkAndAsh/Events/OnItemDecayed")]
#endif
    public class OnItemDecayedEvent : GameEvent<ItemDecayedData> { }

#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
    [UnityEngine.CreateAssetMenu(fileName = "OnQuestActAdvancedEvent", menuName = "InkAndAsh/Events/OnQuestActAdvanced")]
#endif
    public class OnQuestActAdvancedEvent : GameEvent<QuestActData> { }

#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
    [UnityEngine.CreateAssetMenu(fileName = "OnSpireCalibratedEvent", menuName = "InkAndAsh/Events/OnSpireCalibrated")]
#endif
    public class OnSpireCalibratedEvent : GameEvent<SpireCalibratedData> { }
}
