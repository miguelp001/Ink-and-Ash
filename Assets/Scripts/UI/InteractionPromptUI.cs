using UnityEngine;
using TMPro;

namespace InkAndAsh.UI
{
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private GameObject _promptPanel;
        [SerializeField] private TextMeshProUGUI _promptText;

        public static InteractionPromptUI Instance { get; private set; }

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

            HidePrompt();
        }

        public void ShowPrompt(string message)
        {
            if (_promptPanel != null) _promptPanel.SetActive(true);
            if (_promptText != null) _promptText.text = message;
        }

        public void HidePrompt()
        {
            if (_promptPanel != null) _promptPanel.SetActive(false);
        }
    }
}
