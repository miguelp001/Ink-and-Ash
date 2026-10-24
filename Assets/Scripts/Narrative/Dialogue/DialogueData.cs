using System;
using System.Collections.Generic;
using UnityEngine;

namespace InkAndAsh.Narrative.Dialogue
{
    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName;
        [TextArea(2, 5)]
        public string text;
        public string audioClipEventName;
    }

    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public DialogueSequence nextSequence;
        public string actionFlag;
    }

    [CreateAssetMenu(fileName = "NewDialogueSequence", menuName = "Ink and Ash/Dialogue Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        public string sequenceId;
        public List<DialogueLine> lines = new List<DialogueLine>();
        public List<DialogueChoice> choices = new List<DialogueChoice>();
    }
}
