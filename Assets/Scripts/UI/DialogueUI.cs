using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InkAndAsh.Narrative.Dialogue;

namespace InkAndAsh.UI
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _dialoguePanel;
        [SerializeField] private TextMeshProUGUI _speakerNameText;
        [SerializeField] private TextMeshProUGUI _dialogueBodyText;
        [SerializeField] private Button _advanceButton;
        [SerializeField] private Transform _choicesContainer;
        [SerializeField] private Button _choiceButtonPrefab;

        private readonly List<Button> _activeChoiceButtons = new List<Button>();

        private void Start()
        {
            if (DialogueRunner.Instance != null)
            {
                DialogueRunner.Instance.OnLineStarted += HandleLineStarted;
                DialogueRunner.Instance.OnSequenceEnded += HandleSequenceEnded;
            }

            if (_advanceButton != null)
            {
                _advanceButton.onClick.AddListener(OnAdvanceClicked);
            }

            if (_dialoguePanel != null) _dialoguePanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (DialogueRunner.Instance != null)
            {
                DialogueRunner.Instance.OnLineStarted -= HandleLineStarted;
                DialogueRunner.Instance.OnSequenceEnded -= HandleSequenceEnded;
            }
        }

        private void HandleLineStarted(DialogueLine line)
        {
            if (_dialoguePanel != null) _dialoguePanel.SetActive(true);
            if (_speakerNameText != null) _speakerNameText.text = line.speakerName;
            if (_dialogueBodyText != null) _dialogueBodyText.text = line.text;

            ClearChoices();
        }

        private void HandleSequenceEnded(DialogueSequence sequence)
        {
            if (_dialoguePanel != null) _dialoguePanel.SetActive(false);
            ClearChoices();
        }

        private void OnAdvanceClicked()
        {
            if (DialogueRunner.Instance != null)
            {
                DialogueRunner.Instance.AdvanceDialogue();
            }
        }

        private void ClearChoices()
        {
            foreach (var btn in _activeChoiceButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            _activeChoiceButtons.Clear();
        }
    }
}
