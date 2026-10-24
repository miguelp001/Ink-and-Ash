using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using InkAndAsh.Combat;
using InkAndAsh.Narrative.Dialogue;
using InkAndAsh.Prologue;

namespace InkAndAsh.Tests.EditMode
{
    [TestFixture]
    public class ProloguePhase8Tests
    {
        private GameObject _prologueHost;
        private RestorativeCombatController _combatController;
        private PrologueDirector _director;

        [SetUp]
        public void SetUp()
        {
            _prologueHost = new GameObject("PrologueTestHost");
            _combatController = _prologueHost.AddComponent<RestorativeCombatController>();
            _director = _prologueHost.AddComponent<PrologueDirector>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_prologueHost != null)
            {
                Object.DestroyImmediate(_prologueHost);
            }
        }

        [Test]
        public void Stage1_ReceivingSealOfOffice_UnlocksCryptGate()
        {
            Assert.IsFalse(_director.hasReceivedSealOfOffice);
            Assert.IsFalse(_director.isCryptDoorUnlocked);

            _director.OnSacredHearthInteracted();

            Assert.IsTrue(_director.hasReceivedSealOfOffice);
            Assert.IsTrue(_director.isCryptDoorUnlocked);
        }

        [Test]
        public void Stage2_RestorativeCombat_InitializesCombatantsAccurately()
        {
            _combatController.StartRestlessSpiritEncounter();

            Assert.IsTrue(_combatController.IsInCombat);
            Assert.AreEqual(0f, _combatController.unburdenProgress);
            Assert.AreEqual(3, _combatController.Engine.Timeline.Count);

            Assert.IsNotNull(_combatController.Vicar);
            Assert.IsTrue(_combatController.Vicar.IsAlly);
            Assert.AreEqual(50f, _combatController.Vicar.BaseAgility);

            Assert.IsNotNull(_combatController.Companion);
            Assert.IsTrue(_combatController.Companion.IsAlly);

            Assert.IsNotNull(_combatController.RestlessSpirit);
            Assert.IsFalse(_combatController.RestlessSpirit.IsAlly);
            Assert.AreEqual(42f, _combatController.RestlessSpirit.BaseAgility);
        }

        [Test]
        public void Stage2_ActiveListening_AdvancesUnburdeningAndCalmsArousal()
        {
            _combatController.StartRestlessSpiritEncounter();
            float initialArousal = _combatController.spiritArousal;

            _combatController.ExecuteRestorativeAction(RestorativeActionType.ActiveListening);

            Assert.AreEqual(35f, _combatController.unburdenProgress);
            Assert.AreEqual(initialArousal - 25f, _combatController.spiritArousal);
        }

        [Test]
        public void Stage2_RestorativeNegotiation_AdvancesUnburdeningAndElevatesPleasure()
        {
            _combatController.StartRestlessSpiritEncounter();
            float initialPleasure = _combatController.spiritPleasure;

            _combatController.ExecuteRestorativeAction(RestorativeActionType.RestorativeNegotiation);

            Assert.AreEqual(35f, _combatController.unburdenProgress);
            Assert.AreEqual(initialPleasure + 30f, _combatController.spiritPleasure);
        }

        [Test]
        public void Stage2_MediatorsAnchor_GuaranteesSyncMoveAvailability()
        {
            _combatController.StartRestlessSpiritEncounter();

            // Desynchronize timeline turns beyond 2 units
            _combatController.Vicar.NextTurnTime = 1.0f;
            _combatController.Companion.NextTurnTime = 10.0f;

            Assert.IsFalse(_combatController.Engine.CheckSyncMoveAvailable(_combatController.Vicar, _combatController.Companion));

            _combatController.ExecuteRestorativeAction(RestorativeActionType.MediatorsAnchor);

            Assert.IsTrue(_combatController.CanPerformSyncMove());
        }

        [Test]
        public void Stage2_Intervention_ProtectsAllyAndAdvancesTimeline()
        {
            _combatController.StartRestlessSpiritEncounter();
            _combatController.Companion.NextTurnTime = 20.0f;
            float initialVicarFocus = _combatController.Vicar.FocusPoints;

            _combatController.ExecuteRestorativeAction(RestorativeActionType.Intervention);

            Assert.AreEqual(initialVicarFocus - 25f, _combatController.Vicar.FocusPoints);
            Assert.AreEqual(5.0f, _combatController.Companion.NextTurnTime);
        }

        [Test]
        public void Stage2_FinalUnburdenRite_ConcludesEncounterPeacefullyAt100Percent()
        {
            _combatController.StartRestlessSpiritEncounter();
            bool wonPeacefully = false;
            _combatController.OnBattleWonPeacefully += () => wonPeacefully = true;

            // Execute restorative actions to reach 100%
            _combatController.ExecuteRestorativeAction(RestorativeActionType.ActiveListening);
            _combatController.ExecuteRestorativeAction(RestorativeActionType.ActiveListening);
            _combatController.ExecuteRestorativeAction(RestorativeActionType.RestorativeNegotiation);

            Assert.GreaterOrEqual(_combatController.unburdenProgress, 100f);

            _combatController.ExecuteRestorativeAction(RestorativeActionType.FinalUnburdenRite);

            Assert.IsTrue(wonPeacefully);
            Assert.IsFalse(_combatController.IsInCombat);
        }

        [Test]
        public void Stage3_Ordination_RequiresSpiritUnburdenedBeforeOpeningDoors()
        {
            _director.hasReceivedSealOfOffice = true;
            _director.isSpiritUnburdened = false;

            // Attempting to exit temple before ordination
            Assert.IsFalse(_director.isOrdinationComplete);

            // After spirit is unburdened and elder confers ordination
            _director.isSpiritUnburdened = true;
            _director.OnElderOrynInteracted();

            Assert.IsTrue(_director.isOrdinationComplete);
        }
    }
}
