using System;
using UnityEngine;

namespace InkAndAsh.Narrative.Dialogue
{
    public class DialogueRunner : MonoBehaviour
    {
        public static DialogueRunner Instance { get; private set; }

        public event Action<DialogueLine> OnLineStarted;
        public event Action<DialogueSequence> OnSequenceEnded;
        public event Action<string> OnDialogueAction;

        private DialogueSequence _currentSequence;
        private int _currentLineIndex = -1;

        public bool IsDialogueActive => _currentSequence != null;

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

        public void StartDialogue(DialogueSequence sequence)
        {
            if (sequence == null || sequence.lines.Count == 0) return;

            _currentSequence = sequence;
            _currentLineIndex = 0;
            PlayCurrentLine();
        }

        public void AdvanceDialogue()
        {
            if (_currentSequence == null) return;

            _currentLineIndex++;
            if (_currentLineIndex < _currentSequence.lines.Count)
            {
                PlayCurrentLine();
            }
            else
            {
                EndDialogue();
            }
        }

        public void SelectChoice(DialogueChoice choice)
        {
            if (choice == null) return;

            if (!string.IsNullOrEmpty(choice.actionFlag))
            {
                OnDialogueAction?.Invoke(choice.actionFlag);
            }

            if (choice.nextSequence != null)
            {
                StartDialogue(choice.nextSequence);
            }
            else
            {
                EndDialogue();
            }
        }

        private void PlayCurrentLine()
        {
            if (_currentSequence != null && _currentLineIndex >= 0 && _currentLineIndex < _currentSequence.lines.Count)
            {
                OnLineStarted?.Invoke(_currentSequence.lines[_currentLineIndex]);
            }
        }

        private void EndDialogue()
        {
            var finished = _currentSequence;
            _currentSequence = null;
            _currentLineIndex = -1;
            OnSequenceEnded?.Invoke(finished);
        }
    }
}
