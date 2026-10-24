using System;
using System.Collections.Generic;
using NUnit.Framework;
using InkAndAsh.Data;
using InkAndAsh.Narrative;

namespace InkAndAsh.Tests.EditMode
{
    [TestFixture]
    public class QuestManagerTests
    {
        private QuestManager _questManager;
        private QuestData _sampleQuest;

        [SetUp]
        public void SetUp()
        {
            _questManager = new QuestManager();

            _sampleQuest = new QuestData
            {
                questId = "quest_mill_and_measure",
                questTitle = "The Mill and the Measure",
                npcId = "npc_anchor_kaelen",
                targetBiome = "The Grasslands",
                currentAct = QuestAct.Friction,
                acts = new List<QuestActData>
                {
                    new QuestActData
                    {
                        actId = "act_1",
                        act = QuestAct.Friction,
                        title = "Stalled Millstones",
                        description = "Deliver 10 kg Golden Barley to relieve village bread shortage.",
                        completionFlag = "flag_barley_delivered",
                        isCompleted = false
                    },
                    new QuestActData
                    {
                        actId = "act_2",
                        act = QuestAct.Ripple,
                        title = "The Dispute of Grain",
                        description = "Mediate between village elders and young radical millers.",
                        completionFlag = "flag_elders_mediated",
                        isCompleted = false
                    },
                    new QuestActData
                    {
                        actId = "act_3",
                        act = QuestAct.Crisis,
                        title = "Midnight Tithe Inspection",
                        description = "De-escalate the Imperial tax collector inspection.",
                        completionFlag = "flag_inspection_deescalated",
                        isCompleted = false
                    },
                    new QuestActData
                    {
                        actId = "act_4",
                        act = QuestAct.Aftermath,
                        title = "Shared Stores",
                        description = "Establish a communal granary outside the tax ledger.",
                        completionFlag = "flag_granary_established",
                        isCompleted = false
                    },
                    new QuestActData
                    {
                        actId = "act_5",
                        act = QuestAct.Legacy,
                        title = "The Measure of Peace",
                        description = "Seal the grain covenant with regional caravan partners.",
                        completionFlag = "flag_covenant_sealed",
                        isCompleted = false,
                        reward = new QuestReward
                        {
                            insightPoints = 50,
                            unlockedBoon = "Boon_Free_Village_Smithy_Repairs"
                        }
                    }
                }
            };
        }

        #region 1. Quest Lifecycle & Activation

        [Test]
        public void QuestLifecycle_StartQuest_AddsToActiveQuests()
        {
            _questManager.StartQuest(_sampleQuest);

            Assert.That(_questManager.ActiveQuests.ContainsKey("quest_mill_and_measure"), Is.True);
            Assert.That(_questManager.GetQuest("quest_mill_and_measure"), Is.EqualTo(_sampleQuest));
            Assert.That(_questManager.IsQuestCompleted("quest_mill_and_measure"), Is.False);
            Assert.That(_sampleQuest.currentAct, Is.EqualTo(QuestAct.Friction));
        }

        [Test]
        public void QuestLifecycle_DuplicateStart_Ignored()
        {
            _questManager.StartQuest(_sampleQuest);
            Assert.That(_questManager.ActiveQuests.Count, Is.EqualTo(1));

            // Attempt starting same quest again
            _questManager.StartQuest(_sampleQuest);
            Assert.That(_questManager.ActiveQuests.Count, Is.EqualTo(1));
        }

        [Test]
        public void QuestLifecycle_NullOrEmptyQuest_Ignored()
        {
            _questManager.StartQuest(null);
            _questManager.StartQuest(new QuestData { questId = "" });
            Assert.That(_questManager.ActiveQuests.Count, Is.EqualTo(0));
        }

        #endregion

        #region 2. Five-Act State Progression

        [Test]
        public void FiveActProgression_AdvancesThroughAllFiveActsSequentially()
        {
            _questManager.StartQuest(_sampleQuest);

            var recordedTransitions = new List<(QuestAct oldAct, QuestAct newAct)>();
            _questManager.OnQuestActAdvanced += (questId, oldAct, newAct) =>
            {
                recordedTransitions.Add((oldAct, newAct));
            };

            bool completedEventFired = false;
            QuestReward finalReward = null;
            _questManager.OnQuestCompleted += (questId, reward) =>
            {
                completedEventFired = true;
                finalReward = reward;
            };

            // Act 1: Friction -> Act 2: Ripple
            bool adv1 = _questManager.AdvanceQuestAct("quest_mill_and_measure");
            Assert.That(adv1, Is.True);
            Assert.That(_sampleQuest.currentAct, Is.EqualTo(QuestAct.Ripple));
            Assert.That(_sampleQuest.acts[0].isCompleted, Is.True);
            Assert.That(_sampleQuest.actCompletionFlags["flag_barley_delivered"], Is.True);

            // Act 2: Ripple -> Act 3: Crisis
            bool adv2 = _questManager.AdvanceQuestAct("quest_mill_and_measure");
            Assert.That(adv2, Is.True);
            Assert.That(_sampleQuest.currentAct, Is.EqualTo(QuestAct.Crisis));
            Assert.That(_sampleQuest.acts[1].isCompleted, Is.True);
            Assert.That(_sampleQuest.actCompletionFlags["flag_elders_mediated"], Is.True);

            // Act 3: Crisis -> Act 4: Aftermath
            bool adv3 = _questManager.AdvanceQuestAct("quest_mill_and_measure");
            Assert.That(adv3, Is.True);
            Assert.That(_sampleQuest.currentAct, Is.EqualTo(QuestAct.Aftermath));
            Assert.That(_sampleQuest.acts[2].isCompleted, Is.True);
            Assert.That(_sampleQuest.actCompletionFlags["flag_inspection_deescalated"], Is.True);

            // Act 4: Aftermath -> Act 5: Legacy
            bool adv4 = _questManager.AdvanceQuestAct("quest_mill_and_measure");
            Assert.That(adv4, Is.True);
            Assert.That(_sampleQuest.currentAct, Is.EqualTo(QuestAct.Legacy));
            Assert.That(_sampleQuest.acts[3].isCompleted, Is.True);
            Assert.That(_sampleQuest.actCompletionFlags["flag_granary_established"], Is.True);

            // Verify the 4 transition events
            Assert.That(recordedTransitions.Count, Is.EqualTo(4));
            Assert.That(recordedTransitions[0], Is.EqualTo((QuestAct.Friction, QuestAct.Ripple)));
            Assert.That(recordedTransitions[1], Is.EqualTo((QuestAct.Ripple, QuestAct.Crisis)));
            Assert.That(recordedTransitions[2], Is.EqualTo((QuestAct.Crisis, QuestAct.Aftermath)));
            Assert.That(recordedTransitions[3], Is.EqualTo((QuestAct.Aftermath, QuestAct.Legacy)));

            // Act 5: Legacy -> Completion
            bool adv5 = _questManager.AdvanceQuestAct("quest_mill_and_measure");
            Assert.That(adv5, Is.True);
            Assert.That(_sampleQuest.acts[4].isCompleted, Is.True);
            Assert.That(_sampleQuest.actCompletionFlags["flag_covenant_sealed"], Is.True);

            // Verify quest completion state
            Assert.That(completedEventFired, Is.True);
            Assert.That(finalReward, Is.Not.Null);
            Assert.That(finalReward.insightPoints, Is.EqualTo(50));
            Assert.That(finalReward.unlockedBoon, Is.EqualTo("Boon_Free_Village_Smithy_Repairs"));

            Assert.That(_questManager.IsQuestCompleted("quest_mill_and_measure"), Is.True);
            Assert.That(_questManager.ActiveQuests.ContainsKey("quest_mill_and_measure"), Is.False);
            Assert.That(_questManager.CompletedQuests.Contains("quest_mill_and_measure"), Is.True);
        }

        [Test]
        public void QuestProgression_CannotRestartCompletedQuest()
        {
            _questManager.StartQuest(_sampleQuest);

            // Advance through all 5 acts to completion
            for (int i = 0; i < 5; i++)
            {
                _questManager.AdvanceQuestAct("quest_mill_and_measure");
            }

            Assert.That(_questManager.IsQuestCompleted("quest_mill_and_measure"), Is.True);

            // Attempt to re-start completed quest
            _questManager.StartQuest(_sampleQuest);
            Assert.That(_questManager.ActiveQuests.ContainsKey("quest_mill_and_measure"), Is.False);
        }

        [Test]
        public void QuestProgression_AdvanceNonExistentQuest_ReturnsFalse()
        {
            bool result = _questManager.AdvanceQuestAct("missing_quest_id");
            Assert.That(result, Is.False);
        }

        #endregion

        #region 3. Quest Data Schema Validation

        [Test]
        public void QuestData_IsValid_EnforcesRequiredFieldsAndFiveActs()
        {
            Assert.That(_sampleQuest.IsValid(), Is.True);

            // Missing quest ID
            var invalidQuest1 = new QuestData { questId = "", npcId = "npc_1", acts = _sampleQuest.acts };
            Assert.That(invalidQuest1.IsValid(), Is.False);

            // Missing NPC ID
            var invalidQuest2 = new QuestData { questId = "q_1", npcId = "", acts = _sampleQuest.acts };
            Assert.That(invalidQuest2.IsValid(), Is.False);

            // Fewer than 5 acts
            var invalidQuest3 = new QuestData
            {
                questId = "q_1",
                npcId = "npc_1",
                acts = new List<QuestActData> { _sampleQuest.acts[0], _sampleQuest.acts[1] }
            };
            Assert.That(invalidQuest3.IsValid(), Is.False);
        }

        #endregion
    }
}
