using System;
using System.Collections;
using UnityEngine;
using InkAndAsh.Core;
using InkAndAsh.Core.Interaction;
using InkAndAsh.Combat;
using InkAndAsh.Narrative.Dialogue;
using InkAndAsh.UI;

namespace InkAndAsh.Prologue
{
    public enum PrologueStage
    {
        Stage1_AwakeningAndRites,
        Stage2_CryptMeditationClimax,
        Stage3_OrdinationAndPilgrimage
    }

    public class PrologueDirector : MonoBehaviour
    {
        public static PrologueDirector Instance { get; private set; }

        [Header("State")]
        [SerializeField] private PrologueStage _currentStage = PrologueStage.Stage1_AwakeningAndRites;

        [Header("Scene Actors & Transforms")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private Transform _templeAwakeningPoint;
        [SerializeField] private Transform _cryptEntrancePoint;
        [SerializeField] private Transform _cryptArenaPoint;
        [SerializeField] private Transform _overworldThresholdPoint;

        [Header("Interactables")]
        [SerializeField] private Interactable _elderOrynNpc;
        [SerializeField] private Interactable _sacredHearth;
        [SerializeField] private Interactable _cryptDoor;
        [SerializeField] private Interactable _templeExitDoor;

        [Header("Combat & UI")]
        [SerializeField] private RestorativeCombatController _combatController;
        [SerializeField] private RestorativeCombatUI _combatUI;

        [Header("Flags")]
        public bool hasReceivedSealOfOffice;
        public bool isCryptDoorUnlocked;
        public bool isSpiritUnburdened;
        public bool isOrdinationComplete;

        public event Action<PrologueStage> OnStageChanged;

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

        private void Start()
        {
            InitializeStage1();

            if (_combatController != null)
            {
                _combatController.OnBattleWonPeacefully += HandleSpiritUnburdened;
            }
        }

        private void OnDestroy()
        {
            if (_combatController != null)
            {
                _combatController.OnBattleWonPeacefully -= HandleSpiritUnburdened;
            }
        }

        public void InitializeStage1()
        {
            _currentStage = PrologueStage.Stage1_AwakeningAndRites;
            OnStageChanged?.Invoke(_currentStage);

            if (_player != null && _templeAwakeningPoint != null)
            {
                _player.transform.position = _templeAwakeningPoint.position;
            }

            SetupElderDialogue();
        }

        public void OnElderOrynInteracted()
        {
            if (!hasReceivedSealOfOffice)
            {
                StartElderIntroDialogue();
            }
            else if (!isSpiritUnburdened)
            {
                StartElderCryptPromptDialogue();
            }
            else if (!isOrdinationComplete)
            {
                StartElderOrdinationDialogue();
            }
            else
            {
                StartElderFarewellDialogue();
            }
        }

        public void OnSacredHearthInteracted()
        {
            if (!hasReceivedSealOfOffice)
            {
                hasReceivedSealOfOffice = true;
                isCryptDoorUnlocked = true;

                DialogueSequence sealSeq = ScriptableObject.CreateInstance<DialogueSequence>();
                sealSeq.lines.Add(new DialogueLine
                {
                    speakerName = "Sacred Hearth",
                    text = "Warm resin and embers glow in the stone basin. You take up the bronze Vicar's seal. Its weight is gentle in your palm."
                });
                sealSeq.lines.Add(new DialogueLine
                {
                    speakerName = "Elder Oryn",
                    text = "The seal of the Unbound Thread is yours. Now descend to the meditation crypt. An agitated soul wanders there; listen to its sorrow and ease its knot."
                });

                if (DialogueRunner.Instance != null)
                {
                    DialogueRunner.Instance.StartDialogue(sealSeq);
                }
            }
        }

        public void OnCryptDoorInteracted()
        {
            if (!isCryptDoorUnlocked)
            {
                DialogueSequence lockedSeq = ScriptableObject.CreateInstance<DialogueSequence>();
                lockedSeq.lines.Add(new DialogueLine
                {
                    speakerName = "Crypt Gate",
                    text = "The iron-banded gate is barred. Speak with Elder Oryn and take up the seal at the sacred hearth first."
                });
                if (DialogueRunner.Instance != null)
                {
                    DialogueRunner.Instance.StartDialogue(lockedSeq);
                }
                return;
            }

            TransitionToStage2Crypt();
        }

        public void TransitionToStage2Crypt()
        {
            _currentStage = PrologueStage.Stage2_CryptMeditationClimax;
            OnStageChanged?.Invoke(_currentStage);

            if (_player != null && _cryptArenaPoint != null)
            {
                _player.transform.position = _cryptArenaPoint.position;
            }

            StartCoroutine(StartCryptEncounterRoutine());
        }

        private IEnumerator StartCryptEncounterRoutine()
        {
            yield return new WaitForSeconds(1.0f);

            if (_combatController != null)
            {
                _combatController.StartRestlessSpiritEncounter();
            }
        }

        private void HandleSpiritUnburdened()
        {
            isSpiritUnburdened = true;
            StartCoroutine(TransitionToStage3Routine());
        }

        private IEnumerator TransitionToStage3Routine()
        {
            yield return new WaitForSeconds(3.5f);

            _currentStage = PrologueStage.Stage3_OrdinationAndPilgrimage;
            OnStageChanged?.Invoke(_currentStage);

            if (_player != null && _templeAwakeningPoint != null)
            {
                _player.transform.position = _templeAwakeningPoint.position;
            }

            StartElderOrdinationDialogue();
        }

        private void StartElderIntroDialogue()
        {
            DialogueSequence seq = ScriptableObject.CreateInstance<DialogueSequence>();
            seq.lines.Add(new DialogueLine
            {
                speakerName = "Elder Oryn",
                text = "Peace to you, initiate. You awaken at the threshold of the Unbound Thread. Remember our sole charge: not to conquer, but to unburden."
            });
            seq.lines.Add(new DialogueLine
            {
                speakerName = "Elder Oryn",
                text = "The Iron Ledger Empire counts only what it can tax and freeze in place. We answer with care and shared bread. Approach the sacred hearth and take your seal."
            });

            if (DialogueRunner.Instance != null)
            {
                DialogueRunner.Instance.StartDialogue(seq);
            }
        }

        private void StartElderCryptPromptDialogue()
        {
            DialogueSequence seq = ScriptableObject.CreateInstance<DialogueSequence>();
            seq.lines.Add(new DialogueLine
            {
                speakerName = "Elder Oryn",
                text = "The crypt gate stands open below. Remember: do not strike in anger. Active listening and honest negotiation will unravel its anguish."
            });

            if (DialogueRunner.Instance != null)
            {
                DialogueRunner.Instance.StartDialogue(seq);
            }
        }

        private void StartElderOrdinationDialogue()
        {
            DialogueSequence seq = ScriptableObject.CreateInstance<DialogueSequence>();
            seq.lines.Add(new DialogueLine
            {
                speakerName = "Elder Oryn",
                text = "You have eased a wandering soul with patience rather than iron. By the rite of the Unbound Thread, you are now ordained as Vicar-Mediator."
            });
            seq.lines.Add(new DialogueLine
            {
                speakerName = "Elder Oryn",
                text = "Your regional pilgrimage begins. Seven ancient Spires anchor our biomes, yet their beacons are clouded by neglect and imperial strife."
            });
            seq.lines.Add(new DialogueLine
            {
                speakerName = "Elder Oryn",
                text = "Travel the lands, calibrate each Spire beacon and knit our divided communities together. The temple doors stand open."
            });

            isOrdinationComplete = true;

            if (DialogueRunner.Instance != null)
            {
                DialogueRunner.Instance.StartDialogue(seq);
            }
        }

        private void StartElderFarewellDialogue()
        {
            DialogueSequence seq = ScriptableObject.CreateInstance<DialogueSequence>();
            seq.lines.Add(new DialogueLine
            {
                speakerName = "Elder Oryn",
                text = "Walk softly along the roads. Keep your carriage supplies dry and listen to those you meet. May the thread hold."
            });

            if (DialogueRunner.Instance != null)
            {
                DialogueRunner.Instance.StartDialogue(seq);
            }
        }

        public void OnTempleExitDoorInteracted()
        {
            if (!isOrdinationComplete)
            {
                DialogueSequence seq = ScriptableObject.CreateInstance<DialogueSequence>();
                seq.lines.Add(new DialogueLine
                {
                    speakerName = "Sanctuary Door",
                    text = "The heavy oak doors open only once your ordination is complete."
                });
                if (DialogueRunner.Instance != null)
                {
                    DialogueRunner.Instance.StartDialogue(seq);
                }
                return;
            }

            DialogueSequence mapSeq = ScriptableObject.CreateInstance<DialogueSequence>();
            mapSeq.lines.Add(new DialogueLine
            {
                speakerName = "Sanctuary Threshold",
                text = "You step onto the Grasslands road. Before you spreads the tactile overworld parchment map, shrouded in ink-wash mist. The pilgrimage has begun."
            });

            if (DialogueRunner.Instance != null)
            {
                DialogueRunner.Instance.StartDialogue(mapSeq);
            }
        }

        private void SetupElderDialogue()
        {
            if (_elderOrynNpc != null)
            {
                _elderOrynNpc.onInteracted.AddListener(OnElderOrynInteracted);
            }

            if (_sacredHearth != null)
            {
                _sacredHearth.onInteracted.AddListener(OnSacredHearthInteracted);
            }

            if (_cryptDoor != null)
            {
                _cryptDoor.onInteracted.AddListener(OnCryptDoorInteracted);
            }

            if (_templeExitDoor != null)
            {
                _templeExitDoor.onInteracted.AddListener(OnTempleExitDoorInteracted);
            }
        }
    }
}
