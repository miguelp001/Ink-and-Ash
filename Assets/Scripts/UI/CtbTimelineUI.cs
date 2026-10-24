using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InkAndAsh.Combat;

namespace InkAndAsh.UI
{
    public class CtbTimelineUI : MonoBehaviour
    {
        [Header("Timeline Layout")]
        [SerializeField] private Transform _timelineNodeContainer;
        [SerializeField] private GameObject _nodePrefab;

        private readonly List<GameObject> _activeNodes = new List<GameObject>();

        public void RefreshTimeline(IReadOnlyList<Combatant> combatants)
        {
            ClearNodes();

            if (combatants == null || _timelineNodeContainer == null) return;

            for (int i = 0; i < combatants.Count; i++)
            {
                var c = combatants[i];
                GameObject nodeObj;

                if (_nodePrefab != null)
                {
                    nodeObj = Instantiate(_nodePrefab, _timelineNodeContainer);
                }
                else
                {
                    nodeObj = CreateDefaultNode(c, i);
                }

                _activeNodes.Add(nodeObj);
                ConfigureNodeVisuals(nodeObj, c, i);
            }
        }

        private void ConfigureNodeVisuals(GameObject nodeObj, Combatant combatant, int turnIndex)
        {
            var textComp = nodeObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null)
            {
                string tag = combatant.IsAlly ? "[Ally]" : "[Spirit]";
                textComp.text = $"{turnIndex + 1}. {tag} {combatant.Name} (T+{combatant.NextTurnTime:F1})";
                textComp.color = combatant.IsAlly ? new Color(0.4f, 0.9f, 0.5f) : new Color(0.9f, 0.5f, 0.4f);
            }

            var imageComp = nodeObj.GetComponent<Image>();
            if (imageComp != null)
            {
                imageComp.color = turnIndex == 0 ? new Color(1f, 0.9f, 0.5f, 0.9f) : new Color(0.2f, 0.2f, 0.25f, 0.8f);
            }
        }

        private GameObject CreateDefaultNode(Combatant combatant, int turnIndex)
        {
            GameObject node = new GameObject($"Node_{turnIndex}_{combatant.Name}", typeof(RectTransform), typeof(Image));
            node.transform.SetParent(_timelineNodeContainer, false);

            var rt = node.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(240, 32);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(node.transform, false);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.margin = new Vector4(8, 0, 8, 0);

            return node;
        }

        private void ClearNodes()
        {
            foreach (var node in _activeNodes)
            {
                if (node != null) Destroy(node);
            }
            _activeNodes.Clear();
        }
    }
}
