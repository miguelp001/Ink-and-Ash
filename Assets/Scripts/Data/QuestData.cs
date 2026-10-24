using System;
using System.Collections.Generic;

namespace InkAndAsh.Data
{
    public enum QuestAct
    {
        Friction = 1,
        Ripple = 2,
        Crisis = 3,
        Aftermath = 4,
        Legacy = 5
    }

    [Serializable]
    public class QuestReward
    {
        public int insightPoints;
        public PADState padDelta;
        public Dictionary<string, int> itemRewards;
        public string unlockedBoon;

        public QuestReward()
        {
            itemRewards = new Dictionary<string, int>();
        }
    }

    [Serializable]
    public class QuestActData
    {
        public string actId;
        public QuestAct act;
        public string title;
        public string description;
        public string completionFlag;
        public bool isCompleted;
        public QuestReward reward;
    }

    [Serializable]
    public class QuestData
    {
        public string questId;
        public string questTitle;
        public string npcId;
        public string targetBiome;
        public QuestAct currentAct;
        public Dictionary<string, bool> actCompletionFlags;
        public List<QuestActData> acts;

        public QuestData()
        {
            actCompletionFlags = new Dictionary<string, bool>();
            acts = new List<QuestActData>();
        }

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(questId) || string.IsNullOrEmpty(npcId))
            {
                return false;
            }

            if (acts == null || acts.Count != 5)
            {
                return false;
            }

            return true;
        }

        public QuestActData GetCurrentActData()
        {
            if (acts == null) return null;
            return acts.Find(a => a.act == currentAct);
        }

        public bool CompleteCurrentAct()
        {
            var actData = GetCurrentActData();
            if (actData == null) return false;

            actData.isCompleted = true;
            if (!string.IsNullOrEmpty(actData.completionFlag))
            {
                if (actCompletionFlags == null)
                {
                    actCompletionFlags = new Dictionary<string, bool>();
                }
                actCompletionFlags[actData.completionFlag] = true;
            }

            if (currentAct < QuestAct.Legacy)
            {
                currentAct = (QuestAct)((int)currentAct + 1);
                return true;
            }

            return false;
        }
    }
}
