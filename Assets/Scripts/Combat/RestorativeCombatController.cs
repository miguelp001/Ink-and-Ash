using System;
using System.Collections.Generic;
using UnityEngine;

namespace InkAndAsh.Combat
{
    public enum RestorativeActionType
    {
        ActiveListening,
        RestorativeNegotiation,
        MediatorsAnchor,
        SyncMoveSharedCare,
        Intervention,
        FinalUnburdenRite
    }

    public class RestorativeCombatController : MonoBehaviour
    {
        public static RestorativeCombatController Instance { get; private set; }

        public CtbCombatEngine Engine { get; } = new CtbCombatEngine();

        [Header("Combatants")]
        public Combatant Vicar { get; private set; }
        public Combatant Companion { get; private set; }
        public Combatant RestlessSpirit { get; private set; }

        [Header("Restorative State")]
        [Range(0f, 100f)]
        public float unburdenProgress = 0f;
        public float spiritPleasure = -40f;
        public float spiritArousal = 80f;
        public float spiritDominance = 20f;

        [Header("Events")]
        public event Action<Combatant> OnTurnReady;
        public event Action<string, string> OnCombatDialogue;
        public event Action<float> OnUnburdenProgressChanged;
        public event Action OnBattleWonPeacefully;
        public event Action OnBattleLost;

        public bool IsInCombat { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void StartRestlessSpiritEncounter()
        {
            Engine.Clear();
            unburdenProgress = 0f;
            spiritPleasure = -40f;
            spiritArousal = 80f;
            spiritDominance = 20f;

            Vicar = new Combatant
            {
                Id = "vicar_hero",
                Name = "Vicar-Mediator",
                IsAlly = true,
                BaseAgility = 50f,
                MaxHealth = 100f,
                CurrentHealth = 100f,
                FocusPoints = 50f,
                NextTurnTime = 0.5f
            };

            Companion = new Combatant
            {
                Id = "companion_kael",
                Name = "Acolyte Kael",
                IsAlly = true,
                BaseAgility = 45f,
                MaxHealth = 110f,
                CurrentHealth = 110f,
                FocusPoints = 40f,
                NextTurnTime = 1.0f
            };

            RestlessSpirit = new Combatant
            {
                Id = "boss_restless_spirit",
                Name = "The Restless Spirit",
                IsAlly = false,
                BaseAgility = 42f,
                MaxHealth = 200f,
                CurrentHealth = 200f,
                FocusPoints = 0f,
                NextTurnTime = 2.0f
            };

            Engine.AddCombatant(Vicar);
            Engine.AddCombatant(Companion);
            Engine.AddCombatant(RestlessSpirit);

            IsInCombat = true;
            OnUnburdenProgressChanged?.Invoke(unburdenProgress);

            OnCombatDialogue?.Invoke("The Restless Spirit", "Cold iron binds my resting place. Why do you disturb the quiet earth?");
            NotifyNextTurn();
        }

        public bool CanPerformSyncMove()
        {
            return Engine.CheckSyncMoveAvailable(Vicar, Companion);
        }

        public bool CanPerformIntervention()
        {
            return Vicar != null && Vicar.FocusPoints >= 25f;
        }

        public void ExecuteRestorativeAction(RestorativeActionType actionType)
        {
            if (!IsInCombat) return;

            Combatant actor = Engine.Timeline.Count > 0 ? Engine.Timeline[0] : null;
            if (actor == null || !actor.IsAlly) return;

            switch (actionType)
            {
                case RestorativeActionType.ActiveListening:
                    AddUnburdenProgress(35f);
                    spiritArousal = Mathf.Max(-100f, spiritArousal - 25f);
                    OnCombatDialogue?.Invoke(actor.Name, "I hear the weight of your unexpressed grief. Speak, and let the burden soften.");
                    Engine.CommitAction(actor, 20);
                    break;

                case RestorativeActionType.RestorativeNegotiation:
                    AddUnburdenProgress(35f);
                    spiritPleasure = Mathf.Min(100f, spiritPleasure + 30f);
                    OnCombatDialogue?.Invoke(actor.Name, "Your vigil is honored. No imperial ledger can claim your memory.");
                    Engine.CommitAction(actor, 30);
                    break;

                case RestorativeActionType.MediatorsAnchor:
                    Engine.SetMediatorsAnchor(Vicar, Companion);
                    OnCombatDialogue?.Invoke(actor.Name, "We hold this space together. Let our shared focus steady the ground.");
                    Engine.CommitAction(actor, 15);
                    break;

                case RestorativeActionType.SyncMoveSharedCare:
                    if (CanPerformSyncMove())
                    {
                        AddUnburdenProgress(45f);
                        spiritArousal = Mathf.Max(-100f, spiritArousal - 30f);
                        spiritPleasure = Mathf.Min(100f, spiritPleasure + 30f);
                        OnCombatDialogue?.Invoke("Vicar & Acolyte", "Together we lift the fallen stone. Be at ease, traveler of the thread.");
                        Engine.CommitAction(actor, 25);
                    }
                    break;

                case RestorativeActionType.Intervention:
                    if (CanPerformIntervention())
                    {
                        Engine.PerformIntervention(Vicar, Companion);
                        OnCombatDialogue?.Invoke(Vicar.Name, "Step back, Kael. I will shelter this wave.");
                    }
                    break;

                case RestorativeActionType.FinalUnburdenRite:
                    if (unburdenProgress >= 100f)
                    {
                        ResolveBattlePeacefully();
                        return;
                    }
                    break;
            }

            CheckSpiritTurnOrAdvance();
        }

        private void CheckSpiritTurnOrAdvance()
        {
            if (!IsInCombat) return;

            if (unburdenProgress >= 100f)
            {
                OnCombatDialogue?.Invoke("The Restless Spirit", "The coldness recedes. The knot unravels at last.");
            }

            var current = Engine.Timeline.Count > 0 ? Engine.Timeline[0] : null;
            if (current != null && current.Id == RestlessSpirit.Id)
            {
                ExecuteSpiritTurn();
            }
            else
            {
                NotifyNextTurn();
            }
        }

        private void ExecuteSpiritTurn()
        {
            if (!IsInCombat) return;

            if (unburdenProgress < 50f)
            {
                OnCombatDialogue?.Invoke("The Restless Spirit", "The iron encroaches from the hills. They took our names for their lists.");
                // Moderate turn delay on party
                Vicar.CurrentHealth = Mathf.Max(10f, Vicar.CurrentHealth - 8f);
                Companion.CurrentHealth = Mathf.Max(10f, Companion.CurrentHealth - 8f);
                Engine.CommitAction(RestlessSpirit, 35);
            }
            else if (unburdenProgress < 100f)
            {
                OnCombatDialogue?.Invoke("The Restless Spirit", "Is it true? Can one simply set down the tools and rest?");
                Engine.CommitAction(RestlessSpirit, 40);
            }
            else
            {
                OnCombatDialogue?.Invoke("The Restless Spirit", "Peace returns like snow on winter moss.");
                Engine.CommitAction(RestlessSpirit, 50);
            }

            NotifyNextTurn();
        }

        private void AddUnburdenProgress(float delta)
        {
            unburdenProgress = Mathf.Clamp(unburdenProgress + delta, 0f, 100f);
            OnUnburdenProgressChanged?.Invoke(unburdenProgress);
        }

        private void ResolveBattlePeacefully()
        {
            IsInCombat = false;
            OnCombatDialogue?.Invoke("Vicar-Mediator", "All things pass. Return to the earth, friend, and rest unbound.");
            OnBattleWonPeacefully?.Invoke();
        }

        private void NotifyNextTurn()
        {
            if (!IsInCombat) return;

            var next = Engine.Timeline.Count > 0 ? Engine.Timeline[0] : null;
            if (next != null)
            {
                if (next.Id == RestlessSpirit.Id)
                {
                    ExecuteSpiritTurn();
                }
                else
                {
                    OnTurnReady?.Invoke(next);
                }
            }
        }
    }
}
