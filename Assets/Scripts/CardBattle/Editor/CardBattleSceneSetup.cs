using System.IO;
using CardGame.CardBattle.Bridge;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using CardGame.CardBattle.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CardGame.CardBattle.Editor
{
    public static class CardBattleSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/CardBattleScene.unity";
        private const string KaturiFontPath = "Assets/Fonts/Katuri/Katuri SDF.asset";

        [MenuItem("CardGame/CardBattle/Setup CardBattle Scene")]
        public static void SetupScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            EnsureEventSystem();
            var volume = EnsureGlobalVolume();

            var katuriFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KaturiFontPath);

            var canvasGo = CreateCanvasRoot();
            var uiManager = canvasGo.AddComponent<UIManager>();

            var playerCards = CreateCardRow(canvasGo.transform, "PlayerCards", -220f, "PlayerCard");
            var enemyCards = CreateCardRow(canvasGo.transform, "EnemyCards", 220f, "EnemyCard");

            var turnBanner = CreateTurnBanner(canvasGo.transform);
            var playerReserve = CreateLabel(canvasGo.transform, "PlayerReserveText", new Vector2(-760f, -420f), "대기 3");
            var enemyReserve = CreateLabel(canvasGo.transform, "EnemyReserveText", new Vector2(760f, 420f), "대기 3");
            var winPanel = CreateResultPanel(canvasGo.transform, "WinPanel", "승리!", new Color(0.1f, 0.35f, 0.15f, 0.92f));
            var losePanel = CreateResultPanel(canvasGo.transform, "LosePanel", "패배...", new Color(0.35f, 0.1f, 0.1f, 0.92f));

            var systemsGo = new GameObject("BattleSystems");
            var gameManager = systemsGo.AddComponent<GameManager>();
            systemsGo.AddComponent<BattleBridge>();
            systemsGo.AddComponent<BattleSceneBootstrap>();
            var audioAdapter = systemsGo.AddComponent<BattleAudioAdapter>();
            var audioSource = systemsGo.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioAdapter.Configure(null, audioSource);

            WireUIManager(uiManager, canvasGo, turnBanner.text, turnBanner.group,
                playerReserve, enemyReserve, winPanel.panel, losePanel.panel, winPanel.button);
            WireGameManager(gameManager, uiManager, playerCards, enemyCards);
            WireVolume(uiManager, volume);
            WireBridge(systemsGo, gameManager);

            winPanel.panel.SetActive(false);
            losePanel.panel.SetActive(false);

            ApplyFontToAllTmp(canvasGo, katuriFont);
            ApplyTmpFallback(katuriFont);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log("[CardBattle] Scene saved: " + ScenePath);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        private static Volume EnsureGlobalVolume()
        {
            var existing = Object.FindObjectOfType<Volume>();
            if (existing != null)
            {
                return existing;
            }

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
            var go = new GameObject("Global Volume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            return volume;
        }

        private static GameObject CreateCanvasRoot()
        {
            var go = new GameObject("BattleCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static CardView[] CreateCardRow(Transform parent, string rowName, float y, string prefix)
        {
            var row = new GameObject(rowName).AddComponent<RectTransform>();
            row.SetParent(parent, false);
            row.anchorMin = new Vector2(0.5f, 0.5f);
            row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.anchoredPosition = Vector2.zero;

            var views = new CardView[BattleField.SlotCount];
            var xs = new[] { -280f, 0f, 280f };
            for (var i = 0; i < BattleField.SlotCount; i++)
            {
                views[i] = CreateCard(row, prefix + (i + 1), new Vector2(xs[i], y));
            }

            return views;
        }

        private static CardView CreateCard(Transform parent, string name, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(240f, 320f);
            rt.anchoredPosition = pos;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.28f, 1f);
            image.raycastTarget = true;

            var canvasGroup = go.AddComponent<CanvasGroup>();
            var cardView = go.AddComponent<CardView>();

            var nameGo = CreateTmpChild(go.transform, "NameText", new Vector2(0f, 120f), 28, TextAlignmentOptions.Center);
            nameGo.text = name;

            var hpText = CreateTmpChild(go.transform, "HpText", new Vector2(0f, -120f), 24, TextAlignmentOptions.Center);
            hpText.text = "5";

            var slider = CreateHpSlider(go.transform);

            WireCardView(cardView, image, slider, hpText, nameGo, canvasGroup, rt);
            return cardView;
        }

        private static TextMeshProUGUI CreateTmpChild(
            Transform parent,
            string childName,
            Vector2 anchoredPos,
            float fontSize,
            TextAlignmentOptions align)
        {
            var go = new GameObject(childName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220f, 40f);
            rt.anchoredPosition = anchoredPos;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            return tmp;
        }

        private static Slider CreateHpSlider(Transform parent)
        {
            var root = new GameObject("HpSlider", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 24f);
            rt.anchoredPosition = new Vector2(0f, -80f);

            var bg = new GameObject("Background", typeof(RectTransform));
            bg.transform.SetParent(root.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            Stretch(bg.GetComponent<RectTransform>());

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>());

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.85f, 0.35f, 1f);
            Stretch(fill.GetComponent<RectTransform>());

            var slider = root.AddComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fillImg;
            slider.minValue = 0f;
            slider.maxValue = 10f;
            slider.value = 5f;
            slider.interactable = false;
            return slider;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static (TextMeshProUGUI text, CanvasGroup group) CreateTurnBanner(Transform parent)
        {
            var go = new GameObject("TurnBanner", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600f, 100f);
            rt.anchoredPosition = new Vector2(0f, 0f);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);
            bg.raycastTarget = false;

            var group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var tmp = CreateTmpChild(go.transform, "BannerText", Vector2.zero, 48, TextAlignmentOptions.Center);
            tmp.text = "플레이어 턴";
            Stretch(tmp.rectTransform);
            return (tmp, group);
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, Vector2 pos, string text)
        {
            var tmp = CreateTmpChild(parent, name, pos, 32, TextAlignmentOptions.Center);
            tmp.text = text;
            return tmp;
        }

        private static (GameObject panel, Button button) CreateResultPanel(
            Transform parent,
            string name,
            string title,
            Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Stretch(rt);

            var bg = go.AddComponent<Image>();
            bg.color = bgColor;

            var titleTmp = CreateTmpChild(go.transform, "Title", new Vector2(0f, 60f), 56, TextAlignmentOptions.Center);
            titleTmp.text = title;

            var btnGo = new GameObject("RestartButton", typeof(RectTransform));
            btnGo.transform.SetParent(go.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(280f, 64f);
            btnRt.anchoredPosition = new Vector2(0f, -60f);

            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.25f, 0.45f, 0.85f, 1f);
            var button = btnGo.AddComponent<Button>();

            var btnLabel = CreateTmpChild(btnGo.transform, "Text", Vector2.zero, 28, TextAlignmentOptions.Center);
            btnLabel.text = "다시하기";
            Stretch(btnLabel.rectTransform);

            return (go, button);
        }

        private static void WireCardView(
            CardView cardView,
            Image illustration,
            Slider slider,
            TextMeshProUGUI hpText,
            TextMeshProUGUI nameText,
            CanvasGroup canvasGroup,
            RectTransform shakeRoot)
        {
            var so = new SerializedObject(cardView);
            so.FindProperty("illustrationImage").objectReferenceValue = illustration;
            so.FindProperty("hpSlider").objectReferenceValue = slider;
            so.FindProperty("hpText").objectReferenceValue = hpText;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("shakeRoot").objectReferenceValue = shakeRoot;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireUIManager(
            UIManager ui,
            GameObject canvas,
            TextMeshProUGUI turnBannerText,
            CanvasGroup turnBannerGroup,
            TextMeshProUGUI playerReserve,
            TextMeshProUGUI enemyReserve,
            GameObject winPanel,
            GameObject losePanel,
            Button restartButton)
        {
            var so = new SerializedObject(ui);
            so.FindProperty("rootCanvas").objectReferenceValue = canvas.GetComponent<Canvas>();
            so.FindProperty("turnBannerText").objectReferenceValue = turnBannerText;
            so.FindProperty("turnBannerGroup").objectReferenceValue = turnBannerGroup;
            so.FindProperty("playerReserveText").objectReferenceValue = playerReserve;
            so.FindProperty("enemyReserveText").objectReferenceValue = enemyReserve;
            so.FindProperty("winPanel").objectReferenceValue = winPanel;
            so.FindProperty("losePanel").objectReferenceValue = losePanel;
            so.FindProperty("restartButton").objectReferenceValue = restartButton;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireGameManager(
            GameManager gm,
            UIManager ui,
            CardView[] playerCards,
            CardView[] enemyCards)
        {
            var so = new SerializedObject(gm);
            so.FindProperty("uiManager").objectReferenceValue = ui;
            var playerProp = so.FindProperty("playerCardViews");
            playerProp.arraySize = playerCards.Length;
            for (var i = 0; i < playerCards.Length; i++)
            {
                playerProp.GetArrayElementAtIndex(i).objectReferenceValue = playerCards[i];
            }

            var enemyProp = so.FindProperty("enemyCardViews");
            enemyProp.arraySize = enemyCards.Length;
            for (var i = 0; i < enemyCards.Length; i++)
            {
                enemyProp.GetArrayElementAtIndex(i).objectReferenceValue = enemyCards[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireVolume(UIManager ui, Volume volume)
        {
            var so = new SerializedObject(ui);
            so.FindProperty("postProcessVolume").objectReferenceValue = volume;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyFontToAllTmp(GameObject root, TMP_FontAsset font)
        {
            if (font == null)
            {
                Debug.LogWarning("[CardBattle] Katuri SDF not found at " + KaturiFontPath);
                return;
            }

            var tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (var i = 0; i < tmps.Length; i++)
            {
                tmps[i].font = font;
            }
        }

        private static void ApplyTmpFallback(TMP_FontAsset katuriFont)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(
                "Assets/TextMesh Pro/Resources/TMP Settings.asset");
            if (settings == null || katuriFont == null)
            {
                return;
            }

            var fallbackField = typeof(TMP_Settings).GetField(
                "m_fallbackFontAssets",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fallbackField == null)
            {
                return;
            }

            var list = fallbackField.GetValue(settings) as System.Collections.Generic.List<TMP_FontAsset>;
            if (list == null)
            {
                return;
            }

            if (!list.Contains(katuriFont))
            {
                list.Add(katuriFont);
                EditorUtility.SetDirty(settings);
            }
        }

        private static void WireBridge(GameObject systemsGo, GameManager gameManager)
        {
            var bridge = systemsGo.GetComponent<BattleBridge>();
            var bootstrap = systemsGo.GetComponent<BattleSceneBootstrap>();
            var audio = systemsGo.GetComponent<BattleAudioAdapter>();
            var ui = Object.FindObjectOfType<UIManager>();

            var bridgeSo = new SerializedObject(bridge);
            bridgeSo.FindProperty("gameManager").objectReferenceValue = gameManager;
            bridgeSo.FindProperty("audioAdapter").objectReferenceValue = audio;
            bridgeSo.ApplyModifiedPropertiesWithoutUndo();

            var bootSo = new SerializedObject(bootstrap);
            bootSo.FindProperty("gameManager").objectReferenceValue = gameManager;
            bootSo.FindProperty("uiManager").objectReferenceValue = ui;
            bootSo.FindProperty("battleBridge").objectReferenceValue = bridge;
            bootSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            for (var i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    return;
                }
            }

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
