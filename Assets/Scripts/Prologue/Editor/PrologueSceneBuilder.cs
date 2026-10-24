using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using TMPro;

using InkAndAsh.Core;
using InkAndAsh.Core.Interaction;
using InkAndAsh.Combat;
using InkAndAsh.Narrative.Dialogue;
using InkAndAsh.UI;
using InkAndAsh.Prologue;

namespace InkAndAsh.Prologue.Editor
{
    public static class PrologueSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Prologue_GrasslandsTemple.unity";

        [InitializeOnLoadMethod]
        private static void OnEditorLoaded()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath))
                {
                    Debug.Log("[Ink and Ash] Automatically building and loading Prologue_GrasslandsTemple.unity...");
                    BuildPrologueSceneInternal(false);
                }
            };
        }

        [MenuItem("Ink and Ash/Build Prologue Scene (Minutes 0-10)", false, 10)]
        public static void BuildPrologueScene()
        {
            BuildPrologueSceneInternal(true);
        }

        public static void BuildPrologueSceneInternal(bool showDialog)
        {
            string scenesDir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(scenesDir))
            {
                Directory.CreateDirectory(scenesDir);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Lighting & Environment
            SetupLighting();
            Transform sanctuaryArea = CreateSanctuaryEnvironment();
            Transform cryptArea = CreateCryptEnvironment();

            // 2. Waypoints
            GameObject waypointsRoot = new GameObject("Waypoints");
            Transform awakeningPoint = CreateWaypoint(waypointsRoot.transform, "AwakeningPoint", new Vector3(0, 0.1f, -4f));
            Transform cryptEntrancePoint = CreateWaypoint(waypointsRoot.transform, "CryptEntrancePoint", new Vector3(0, 0.1f, 8f));
            Transform cryptArenaPoint = CreateWaypoint(waypointsRoot.transform, "CryptArenaPoint", new Vector3(0, -5.9f, 25f));
            Transform overworldThresholdPoint = CreateWaypoint(waypointsRoot.transform, "OverworldThresholdPoint", new Vector3(0, 0.1f, -9.5f));

            // 3. Player Setup
            PlayerController player = CreatePlayer(awakeningPoint.position);

            // 4. Camera Rig (Cinemachine 3)
            SetupCameraRig(player.transform);

            // 5. Interactables
            Interactable elderOryn = CreateInteractable("Elder Oryn", new Vector3(0, 0.5f, 0), "Speak with Elder Oryn", new Color(0.3f, 0.7f, 0.9f));
            Interactable sacredHearth = CreateInteractable("Sacred Hearth", new Vector3(4f, 0.4f, 2f), "Inspect Sacred Hearth & Take Seal", new Color(1f, 0.6f, 0.2f), isCylinder: true);
            Interactable cryptGate = CreateInteractable("Crypt Gate", new Vector3(0, 1.0f, 9f), "Descend to Meditation Crypt", new Color(0.5f, 0.5f, 0.6f), isDoor: true);
            Interactable templeDoor = CreateInteractable("Sanctuary Exit Doors", new Vector3(0, 1.0f, -10f), "Step into the Overworld", new Color(0.6f, 0.4f, 0.2f), isDoor: true);

            // 6. Restless Spirit (Crypt Boss)
            GameObject spiritObj = CreateRestlessSpirit(new Vector3(0, -4.5f, 32f));

            // 7. UI Canvas & Hierarchy
            GameObject canvasObj = CreateCanvasHierarchy(out DialogueUI dialogueUI, out RestorativeCombatUI combatUI, out InteractionPromptUI promptUI);

            // 8. Core Managers & Prologue Director
            GameObject managersRoot = new GameObject("Managers");
            managersRoot.AddComponent<GameManager>();
            DialogueRunner dialogueRunner = managersRoot.AddComponent<DialogueRunner>();
            RestorativeCombatController combatController = managersRoot.AddComponent<RestorativeCombatController>();

            PrologueDirector director = managersRoot.AddComponent<PrologueDirector>();
            WireDirector(director, player, awakeningPoint, cryptEntrancePoint, cryptArenaPoint, overworldThresholdPoint,
                         elderOryn, sacredHearth, cryptGate, templeDoor, combatController, combatUI);

            // 9. Bake NavMesh
            BakeNavMesh(sanctuaryArea, cryptArea);

            // 10. Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[Ink and Ash] Successfully built and loaded {ScenePath}!");

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Ink and Ash: Prologue Scene", 
                    "The complete Prologue Scene (Minutes 0-10) has been built and saved to:\n" + ScenePath + 
                    "\n\nYou can now hit Play to experience the full vertical slice loop!", "Open Scene");
            }

            PrologueDirector loadedDirector = UnityEngine.Object.FindAnyObjectByType<PrologueDirector>();
            if (loadedDirector != null)
            {
                Selection.activeGameObject = loadedDirector.gameObject;
            }
        }

        private static void SetupLighting()
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.25f, 0.28f, 0.35f);
        }

        private static Transform CreateSanctuaryEnvironment()
        {
            GameObject root = new GameObject("Sanctuary_Temple_Environment");

            // Temple Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "TempleFloor";
            floor.transform.SetParent(root.transform);
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2.5f, 1f, 2.5f); // 25x25m

            // Temple Pillars
            for (int x = -10; x <= 10; x += 10)
            {
                for (int z = -8; z <= 8; z += 4)
                {
                    if (x == 0) continue;
                    GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pillar.name = $"Pillar_{x}_{z}";
                    pillar.transform.SetParent(root.transform);
                    pillar.transform.position = new Vector3(x, 2.5f, z);
                    pillar.transform.localScale = new Vector3(0.8f, 2.5f, 0.8f);
                }
            }

            var surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            return root.transform;
        }

        private static Transform CreateCryptEnvironment()
        {
            GameObject root = new GameObject("Meditation_Crypt_Environment");

            // Crypt Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "CryptFloor";
            floor.transform.SetParent(root.transform);
            floor.transform.position = new Vector3(0, -6f, 28f);
            floor.transform.localScale = new Vector3(2.0f, 1f, 2.0f); // 20x20m

            // Connecting Ramp / Stairwell
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "DescentRamp";
            ramp.transform.SetParent(root.transform);
            ramp.transform.position = new Vector3(0, -3f, 13.5f);
            ramp.transform.localScale = new Vector3(4f, 0.4f, 10f);
            ramp.transform.rotation = Quaternion.Euler(30f, 0, 0);

            var surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            return root.transform;
        }

        private static Transform CreateWaypoint(Transform parent, string name, Vector3 pos)
        {
            GameObject wp = new GameObject(name);
            wp.transform.SetParent(parent);
            wp.transform.position = pos;
            return wp.transform;
        }

        private static PlayerController CreatePlayer(Vector3 startPos)
        {
            GameObject playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObj.name = "Player_VicarMediator";
            playerObj.tag = "Player";
            playerObj.transform.position = startPos;

            // NavMeshAgent
            NavMeshAgent agent = playerObj.AddComponent<NavMeshAgent>();
            agent.speed = 4.5f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.2f;

            // PlayerController
            PlayerController controller = playerObj.AddComponent<PlayerController>();

            // Distinct player tint
            Renderer rend = playerObj.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.85f, 0.75f, 0.55f);
                rend.material = mat;
            }

            return controller;
        }

        private static void SetupCameraRig(Transform target)
        {
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            camObj.AddComponent<AudioListener>();

            // CinemachineBrain
            camObj.AddComponent<CinemachineBrain>();

            // Cinemachine Virtual Camera
            GameObject vcamObj = new GameObject("CM_ThirdPersonOrbitCamera");
            CinemachineCamera cmCam = vcamObj.AddComponent<CinemachineCamera>();
            cmCam.Target.TrackingTarget = target;
            cmCam.Target.LookAtTarget = target;

            // Elevated cozy JRPG 45-degree isometric follow
            CinemachineFollow follow = vcamObj.AddComponent<CinemachineFollow>();
            follow.FollowOffset = new Vector3(0f, 6.5f, -6.5f);
            vcamObj.AddComponent<CinemachineHardLookAt>();

            vcamObj.transform.position = target.position + follow.FollowOffset;
            vcamObj.transform.LookAt(target.position + Vector3.up * 1.0f);
        }

        private static Interactable CreateInteractable(string name, Vector3 pos, string prompt, Color color, bool isCylinder = false, bool isDoor = false)
        {
            GameObject obj = GameObject.CreatePrimitive(isCylinder ? PrimitiveType.Cylinder : (isDoor ? PrimitiveType.Cube : PrimitiveType.Capsule));
            obj.name = name;
            obj.transform.position = pos;

            if (isDoor)
            {
                obj.transform.localScale = new Vector3(3f, 3f, 0.4f);
            }
            else if (isCylinder)
            {
                obj.transform.localScale = new Vector3(1.6f, 0.5f, 1.6f);
            }

            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = color;
                rend.material = mat;
            }

            Interactable interactable = obj.AddComponent<Interactable>();

            var fieldPrompt = typeof(Interactable).GetField("_promptText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldPrompt != null) fieldPrompt.SetValue(interactable, prompt);

            var fieldDist = typeof(Interactable).GetField("_interactionDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldDist != null) fieldDist.SetValue(interactable, 3.0f);

            var fieldMag = typeof(Interactable).GetField("_magnetismRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldMag != null) fieldMag.SetValue(interactable, 4.5f);

            return interactable;
        }

        private static GameObject CreateRestlessSpirit(Vector3 pos)
        {
            GameObject spirit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spirit.name = "RestlessSpirit_Boss";
            spirit.transform.position = pos;
            spirit.transform.localScale = new Vector3(2f, 2.5f, 2f);

            Renderer rend = spirit.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.4f, 0.7f, 1f, 0.75f);
                rend.material = mat;
            }

            // Light pulse
            GameObject lightChild = new GameObject("SpiritGlow");
            lightChild.transform.SetParent(spirit.transform, false);
            Light l = lightChild.AddComponent<Light>();
            l.color = new Color(0.4f, 0.7f, 1f);
            l.range = 8f;
            l.intensity = 2f;

            return spirit;
        }

        private static GameObject CreateCanvasHierarchy(out DialogueUI dialogueUI, out RestorativeCombatUI combatUI, out InteractionPromptUI promptUI)
        {
            GameObject canvasObj = new GameObject("UI_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // 1. Dialogue Panel
            dialogueUI = CreateDialoguePanel(canvasObj.transform);

            // 2. Restorative Combat Panel
            combatUI = CreateCombatPanel(canvasObj.transform);

            // 3. Interaction Prompt Panel
            promptUI = CreatePromptPanel(canvasObj.transform);

            return canvasObj;
        }

        private static DialogueUI CreateDialoguePanel(Transform parent)
        {
            GameObject panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.05f);
            rt.anchorMax = new Vector2(0.85f, 0.30f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.92f);

            // Speaker Text
            GameObject speakerObj = CreateTextObject(panel.transform, "SpeakerName", new Vector2(0.04f, 0.75f), new Vector2(0.96f, 0.95f), "Speaker", 20, Color.yellow);
            // Body Text
            GameObject bodyObj = CreateTextObject(panel.transform, "BodyText", new Vector2(0.04f, 0.25f), new Vector2(0.96f, 0.72f), "Dialogue text will appear here...", 18, Color.white);

            // Advance Button
            GameObject btnObj = new GameObject("AdvanceButton", typeof(RectTransform), typeof(Button), typeof(Image));
            btnObj.transform.SetParent(panel.transform, false);
            var btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.82f, 0.08f);
            btnRt.anchorMax = new Vector2(0.96f, 0.28f);
            btnRt.offsetMin = btnRt.offsetMax = Vector2.zero;
            btnObj.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.65f);
            CreateTextObject(btnObj.transform, "Text", Vector2.zero, Vector2.one, "Continue", 16, Color.white);

            DialogueUI ui = panel.AddComponent<DialogueUI>();
            SetPrivateField(ui, "_dialoguePanel", panel);
            SetPrivateField(ui, "_speakerNameText", speakerObj.GetComponent<TextMeshProUGUI>());
            SetPrivateField(ui, "_dialogueBodyText", bodyObj.GetComponent<TextMeshProUGUI>());
            SetPrivateField(ui, "_advanceButton", btnObj.GetComponent<Button>());

            panel.SetActive(false);
            return ui;
        }

        private static RestorativeCombatUI CreateCombatPanel(Transform parent)
        {
            GameObject combatPanel = new GameObject("RestorativeCombatPanel", typeof(RectTransform));
            combatPanel.transform.SetParent(parent, false);
            var panelRt = combatPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;

            // Timeline container (left margin)
            GameObject timelineRoot = new GameObject("CtbTimeline", typeof(RectTransform), typeof(Image));
            timelineRoot.transform.SetParent(combatPanel.transform, false);
            var tlRt = timelineRoot.GetComponent<RectTransform>();
            tlRt.anchorMin = new Vector2(0.02f, 0.35f);
            tlRt.anchorMax = new Vector2(0.18f, 0.95f);
            tlRt.offsetMin = tlRt.offsetMax = Vector2.zero;
            timelineRoot.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.85f);

            GameObject nodesContainer = new GameObject("NodesContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            nodesContainer.transform.SetParent(timelineRoot.transform, false);
            var ncRt = nodesContainer.GetComponent<RectTransform>();
            ncRt.anchorMin = new Vector2(0.05f, 0.05f);
            ncRt.anchorMax = new Vector2(0.95f, 0.95f);
            ncRt.offsetMin = ncRt.offsetMax = Vector2.zero;
            var layout = nodesContainer.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CtbTimelineUI timelineUI = timelineRoot.AddComponent<CtbTimelineUI>();
            SetPrivateField(timelineUI, "_timelineNodeContainer", nodesContainer.transform);

            // Unburden Meter (top center)
            GameObject meterObj = new GameObject("UnburdenGauge", typeof(RectTransform), typeof(Slider), typeof(Image));
            meterObj.transform.SetParent(combatPanel.transform, false);
            var meterRt = meterObj.GetComponent<RectTransform>();
            meterRt.anchorMin = new Vector2(0.35f, 0.88f);
            meterRt.anchorMax = new Vector2(0.65f, 0.94f);
            meterRt.offsetMin = meterRt.offsetMax = Vector2.zero;
            meterObj.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);
            Slider slider = meterObj.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            GameObject percentObj = CreateTextObject(meterObj.transform, "PercentText", Vector2.zero, Vector2.one, "Restoration: 0%", 16, Color.cyan);

            // Action Buttons Panel (bottom center)
            GameObject actionsBar = new GameObject("ActionBar", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            actionsBar.transform.SetParent(combatPanel.transform, false);
            var abRt = actionsBar.GetComponent<RectTransform>();
            abRt.anchorMin = new Vector2(0.20f, 0.04f);
            abRt.anchorMax = new Vector2(0.80f, 0.16f);
            abRt.offsetMin = abRt.offsetMax = Vector2.zero;
            actionsBar.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.9f);
            var hLayout = actionsBar.GetComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 8;
            hLayout.padding = new RectOffset(8, 8, 8, 8);
            hLayout.childForceExpandHeight = true;
            hLayout.childForceExpandWidth = true;

            Button btnListen = CreateActionButton(actionsBar.transform, "Active Listening", new Color(0.2f, 0.6f, 0.4f));
            Button btnNegotiate = CreateActionButton(actionsBar.transform, "Negotiation", new Color(0.3f, 0.5f, 0.7f));
            Button btnAnchor = CreateActionButton(actionsBar.transform, "Anchor", new Color(0.6f, 0.4f, 0.7f));
            Button btnSync = CreateActionButton(actionsBar.transform, "Sync Move", new Color(0.8f, 0.6f, 0.2f));
            Button btnIntervene = CreateActionButton(actionsBar.transform, "Intervention", new Color(0.7f, 0.3f, 0.3f));
            Button btnRite = CreateActionButton(actionsBar.transform, "Final Rite", new Color(0.9f, 0.8f, 0.3f));

            // Narrative Banner (middle top)
            GameObject tickerObj = new GameObject("TickerBanner", typeof(RectTransform), typeof(Image));
            tickerObj.transform.SetParent(combatPanel.transform, false);
            var tbRt = tickerObj.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0.25f, 0.76f);
            tbRt.anchorMax = new Vector2(0.75f, 0.85f);
            tbRt.offsetMin = tbRt.offsetMax = Vector2.zero;
            tickerObj.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.8f);

            GameObject spkObj = CreateTextObject(tickerObj.transform, "Speaker", new Vector2(0.04f, 0.55f), new Vector2(0.96f, 0.95f), "The Restless Spirit", 15, Color.yellow);
            GameObject msgObj = CreateTextObject(tickerObj.transform, "Message", new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.55f), "...", 15, Color.white);

            RestorativeCombatUI ui = combatPanel.AddComponent<RestorativeCombatUI>();
            SetPrivateField(ui, "_combatPanel", combatPanel);
            SetPrivateField(ui, "_timelineUI", timelineUI);
            SetPrivateField(ui, "_activeListeningButton", btnListen);
            SetPrivateField(ui, "_negotiationButton", btnNegotiate);
            SetPrivateField(ui, "_anchorButton", btnAnchor);
            SetPrivateField(ui, "_syncMoveButton", btnSync);
            SetPrivateField(ui, "_interventionButton", btnIntervene);
            SetPrivateField(ui, "_unburdenRiteButton", btnRite);
            SetPrivateField(ui, "_unburdenSlider", slider);
            SetPrivateField(ui, "_unburdenPercentText", percentObj.GetComponent<TextMeshProUGUI>());
            SetPrivateField(ui, "_speakerText", spkObj.GetComponent<TextMeshProUGUI>());
            SetPrivateField(ui, "_dialogueBodyText", msgObj.GetComponent<TextMeshProUGUI>());

            combatPanel.SetActive(false);
            return ui;
        }

        private static InteractionPromptUI CreatePromptPanel(Transform parent)
        {
            GameObject promptPanel = new GameObject("InteractionPrompt", typeof(RectTransform), typeof(Image));
            promptPanel.transform.SetParent(parent, false);
            var rt = promptPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.35f, 0.22f);
            rt.anchorMax = new Vector2(0.65f, 0.28f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            promptPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.85f);

            GameObject textObj = CreateTextObject(promptPanel.transform, "PromptText", Vector2.zero, Vector2.one, "Press [Left Click] or Tap to Interact", 18, Color.white);

            InteractionPromptUI ui = promptPanel.AddComponent<InteractionPromptUI>();
            SetPrivateField(ui, "_promptPanel", promptPanel);
            SetPrivateField(ui, "_promptText", textObj.GetComponent<TextMeshProUGUI>());

            promptPanel.SetActive(false);
            return ui;
        }

        private static Button CreateActionButton(Transform parent, string title, Color color)
        {
            GameObject btnObj = new GameObject($"Btn_{title}", typeof(RectTransform), typeof(Button), typeof(Image));
            btnObj.transform.SetParent(parent, false);
            btnObj.GetComponent<Image>().color = color;
            CreateTextObject(btnObj.transform, "Text", Vector2.zero, Vector2.one, title, 14, Color.white);
            return btnObj.GetComponent<Button>();
        }

        private static GameObject CreateTextObject(Transform parent, string name, Vector2 min, Vector2 max, string defaultText, float fontSize, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = defaultText;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return obj;
        }

        private static void WireDirector(PrologueDirector director, PlayerController player, Transform awaken, Transform cryptEnt, Transform cryptArena, Transform overworld,
                                         Interactable elder, Interactable hearth, Interactable gate, Interactable door,
                                         RestorativeCombatController combatCtrl, RestorativeCombatUI combatUI)
        {
            SetPrivateField(director, "_player", player);
            SetPrivateField(director, "_templeAwakeningPoint", awaken);
            SetPrivateField(director, "_cryptEntrancePoint", cryptEnt);
            SetPrivateField(director, "_cryptArenaPoint", cryptArena);
            SetPrivateField(director, "_overworldThresholdPoint", overworld);
            SetPrivateField(director, "_elderOrynNpc", elder);
            SetPrivateField(director, "_sacredHearth", hearth);
            SetPrivateField(director, "_cryptDoor", gate);
            SetPrivateField(director, "_templeExitDoor", door);
            SetPrivateField(director, "_combatController", combatCtrl);
            SetPrivateField(director, "_combatUI", combatUI);
        }

        private static void BakeNavMesh(Transform sanctuary, Transform crypt)
        {
            var surfaces = UnityEngine.Object.FindObjectsByType<NavMeshSurface>();
            foreach (var s in surfaces)
            {
                s.BuildNavMesh();
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
