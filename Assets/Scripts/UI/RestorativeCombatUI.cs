using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InkAndAsh.Combat;

namespace InkAndAsh.UI
{
    public class RestorativeCombatUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject _combatPanel;
        [SerializeField] private CtbTimelineUI _timelineUI;

        [Header("Action Buttons")]
        [SerializeField] private Button _activeListeningButton;
        [SerializeField] private Button _negotiationButton;
        [SerializeField] private Button _anchorButton;
        [SerializeField] private Button _syncMoveButton;
        [SerializeField] private Button _interventionButton;
        [SerializeField] private Button _unburdenRiteButton;

        [Header("Unburden Gauge")]
        [SerializeField] private Slider _unburdenSlider;
        [SerializeField] private TextMeshProUGUI _unburdenPercentText;

        [Header("Dialogue / Ticker")]
        [SerializeField] private TextMeshProUGUI _speakerText;
        [SerializeField] private TextMeshProUGUI _dialogueBodyText;

        private RestorativeCombatController _controller;

        private void Start()
        {
            _controller = RestorativeCombatController.Instance;
            if (_controller != null)
            {
                _controller.OnTurnReady += HandleTurnReady;
                _controller.OnCombatDialogue += HandleCombatDialogue;
                _controller.OnUnburdenProgressChanged += HandleUnburdenChanged;
                _controller.OnBattleWonPeacefully += HandleBattleWon;
            }

            SetupButtonListeners();
            if (_combatPanel != null) _combatPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnTurnReady -= HandleTurnReady;
                _controller.OnCombatDialogue -= HandleCombatDialogue;
                _controller.OnUnburdenProgressChanged -= HandleUnburdenChanged;
                _controller.OnBattleWonPeacefully -= HandleBattleWon;
            }
        }

        private void SetupButtonListeners()
        {
            if (_activeListeningButton != null)
                _activeListeningButton.onClick.AddListener(() => _controller.ExecuteRestorativeAction(RestorativeActionType.ActiveListening));

            if (_negotiationButton != null)
                _negotiationButton.onClick.AddListener(() => _controller.ExecuteRestorativeAction(RestorativeActionType.RestorativeNegotiation));

            if (_anchorButton != null)
                _anchorButton.onClick.AddListener(() => _controller.ExecuteRestorativeAction(RestorativeActionType.MediatorsAnchor));

            if (_syncMoveButton != null)
                _syncMoveButton.onClick.AddListener(() => _controller.ExecuteRestorativeAction(RestorativeActionType.SyncMoveSharedCare));

            if (_interventionButton != null)
                _interventionButton.onClick.AddListener(() => _controller.ExecuteRestorativeAction(RestorativeActionType.Intervention));

            if (_unburdenRiteButton != null)
                _unburdenRiteButton.onClick.AddListener(() => _controller.ExecuteRestorativeAction(RestorativeActionType.FinalUnburdenRite));
        }

        public void OpenCombatUI()
        {
            if (_combatPanel != null) _combatPanel.SetActive(true);
            UpdateTimeline();
        }

        public void CloseCombatUI()
        {
            if (_combatPanel != null) _combatPanel.SetActive(false);
        }

        private void HandleTurnReady(Combatant currentActor)
        {
            OpenCombatUI();
            UpdateTimeline();
            UpdateButtonStates(currentActor);
        }

        private void UpdateButtonStates(Combatant currentActor)
        {
            bool isPlayerTurn = currentActor != null && currentActor.IsAlly;

            if (_activeListeningButton != null) _activeListeningButton.interactable = isPlayerTurn;
            if (_negotiationButton != null) _negotiationButton.interactable = isPlayerTurn;
            if (_anchorButton != null) _anchorButton.interactable = isPlayerTurn;

            if (_syncMoveButton != null)
            {
                _syncMoveButton.interactable = isPlayerTurn && _controller.CanPerformSyncMove();
            }

            if (_interventionButton != null)
            {
                _interventionButton.interactable = isPlayerTurn && _controller.CanPerformIntervention();
            }

            if (_unburdenRiteButton != null)
            {
                bool canUnburden = isPlayerTurn && _controller.unburdenProgress >= 100f;
                _unburdenRiteButton.interactable = canUnburden;
                _unburdenRiteButton.gameObject.SetActive(canUnburden);
            }
        }

        private void HandleCombatDialogue(string speaker, string message)
        {
            if (_speakerText != null) _speakerText.text = speaker;
            if (_dialogueBodyText != null) _dialogueBodyText.text = message;
        }

        private void HandleUnburdenChanged(float progress)
        {
            if (_unburdenSlider != null) _unburdenSlider.value = progress / 100f;
            if (_unburdenPercentText != null) _unburdenPercentText.text = $"Restoration: {progress:F0}%";
        }

        private void HandleBattleWon()
        {
            if (_speakerText != null) _speakerText.text = "Restorative Climax Complete";
            if (_dialogueBodyText != null) _dialogueBodyText.text = "The restless spirit is unburdened and rests in peace.";
            UpdateButtonStates(null);
            Invoke(nameof(CloseCombatUI), 3.0f);
        }

        private void UpdateTimeline()
        {
            if (_timelineUI != null && _controller != null)
            {
                _timelineUI.RefreshTimeline(_controller.Engine.Timeline);
            }
        }
    }
}
