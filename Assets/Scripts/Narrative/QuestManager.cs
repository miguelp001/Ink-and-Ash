using System;
using System.Collections.Generic;
using InkAndAsh.Data;

namespace InkAndAsh.Narrative
{
    public class QuestManager
    {
        private static QuestManager _instance;
        public static QuestManager Instance => _instance ??= new QuestManager();

        private readonly Dictionary<string, QuestData> _activeQuests = new Dictionary<string, QuestData>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _completedQuests = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, QuestData> ActiveQuests => _activeQuests;
        public IReadOnlyCollection<string> CompletedQuests => _completedQuests;

        public event Action<string, QuestAct, QuestAct> OnQuestActAdvanced;
        public event Action<string, QuestReward> OnQuestCompleted;

        public void StartQuest(QuestData quest)
        {
            if (quest == null || string.IsNullOrEmpty(quest.questId)) return;
            if (_activeQuests.ContainsKey(quest.questId) || _completedQuests.Contains(quest.questId)) return;

            _activeQuests[quest.questId] = quest;
        }

        public QuestData GetQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;
            _activeQuests.TryGetValue(questId, out var quest);
            return quest;
        }

        public bool AdvanceQuestAct(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return false;
            if (!_activeQuests.TryGetValue(questId, out var quest)) return false;

            QuestAct oldAct = quest.currentAct;
            var currentActData = quest.GetCurrentActData();

            if (currentActData != null)
            {
                currentActData.isCompleted = true;
                if (!string.IsNullOrEmpty(currentActData.completionFlag))
                {
                    quest.actCompletionFlags[currentActData.completionFlag] = true;
                }
            }

            if (quest.currentAct == QuestAct.Legacy)
            {
                // Final act completed!
                _activeQuests.Remove(questId);
                _completedQuests.Add(questId);

                var finalReward = currentActData?.reward;
                OnQuestCompleted?.Invoke(questId, finalReward);
                return true;
            }

            // Advance to next act
            quest.currentAct = (QuestAct)((int)quest.currentAct + 1);
            OnQuestActAdvanced?.Invoke(questId, oldAct, quest.currentAct);
            return true;
        }

        public bool IsQuestCompleted(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return false;
            return _completedQuests.Contains(questId);
        }
    }
}

#if STANDALONE_TEST
namespace InkAndAsh.StandaloneTests
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using InkAndAsh.Data;
    using InkAndAsh.Narrative;

    [TestFixture]
    public class QuestManagerProductionTests
    {
        private QuestManager _questManager;
        private QuestData _sampleQuest;

        [SetUp]
        public void Setup()
        {
            _questManager = new QuestManager();

            _sampleQuest = new QuestData
            {
                questId = "quest_test_grain",
                questTitle = "The Stalled Millstones",
                npcId = "npc_kaelen",
                currentAct = QuestAct.Friction,
                acts = new List<QuestActData>
                {
                    new QuestActData { actId = "a1", act = QuestAct.Friction, title = "Friction", completionFlag = "Flag_Act1" },
                    new QuestActData { actId = "a2", act = QuestAct.Ripple, title = "Ripple", completionFlag = "Flag_Act2" },
                    new QuestActData { actId = "a3", act = QuestAct.Crisis, title = "Crisis", completionFlag = "Flag_Act3" },
                    new QuestActData { actId = "a4", act = QuestAct.Aftermath, title = "Aftermath", completionFlag = "Flag_Act4" },
                    new QuestActData
                    {
                        actId = "a5",
                        act = QuestAct.Legacy,
                        title = "Legacy",
                        completionFlag = "Flag_Act5",
                        reward = new QuestReward { unlockedBoon = "Boon_Free_Village_Smithy_Repairs", insightPoints = 50 }
                    }
                }
            };
        }

        [Test]
        public void QuestManager_5ActProgression_ToLegacy()
        {
            _questManager.StartQuest(_sampleQuest);
            Assert.That(_questManager.ActiveQuests.ContainsKey("quest_test_grain"), Is.True);

            // Advance Friction -> Ripple
            bool adv1 = _questManager.AdvanceQuestAct("quest_test_grain");
            Assert.That(adv1, Is.True);
            Assert.That(_sampleQuest.currentAct, Is.EqualTo(QuestAct.Ripple));
            Assert.That(_sampleQuest.actCompletionFlags["Flag_Act1"], Is.True);

            // Advance Ripple -> Crisis
            _questManager.AdvanceQuestAct("quest_test_grain");
            Assert.That(_sampleQuest.currentAct, Is.EqualTo(QuestAct.Crisis));

            // Advance Crisis -> Aftermath
            _questManager.AdvanceQuestAct("quest_test_grain");
            Assert.That(_sampleQuest.currentAct, Is.EqualTo(QuestAct.Aftermath));

            // Advance Aftermath -> Legacy
            _questManager.AdvanceQuestAct("quest_test_grain");
            Assert.That(_sampleQuest.currentAct, Is.EqualTo(QuestAct.Legacy));

            // Complete Legacy
            QuestReward rewarded = null;
            _questManager.OnQuestCompleted += (id, r) => rewarded = r;

            bool advFinal = _questManager.AdvanceQuestAct("quest_test_grain");
            Assert.That(advFinal, Is.True);
            Assert.That(_questManager.IsQuestCompleted("quest_test_grain"), Is.True);
            Assert.That(rewarded, Is.Not.Null);
            Assert.That(rewarded.unlockedBoon, Is.EqualTo("Boon_Free_Village_Smithy_Repairs"));
        }
    }
}
#endif
