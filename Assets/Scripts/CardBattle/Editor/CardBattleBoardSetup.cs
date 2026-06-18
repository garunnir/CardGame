using System.IO;
using CardGame.CardBattle.Bridge;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using CardGame.CardBattle.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CardGame.CardBattle.Editor
{
    public static class CardBattleBoardSetup
    {
        public const string PrefabPath = "Assets/Prefabs/CardBattle/CardEntity.prefab";
        public const string LayoutPath = "Assets/Resources/CardBattle/BattleLayout_Default.asset";
        public const string CardFaceMaterialPath = "Assets/Materials/CardBattle/CardFace_Default.mat";
        private const string DefaultSpriteMaterialGuid = "9dfc825aed78fcd4ba02077103263b40";

        private static Mesh builtinQuad;

        [MenuItem("CardGame/CardBattle/Create Card Entity Prefab")]
        public static void CreateCardEntityPrefabMenu()
        {
            RebuildCardEntityPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log("[CardBattle] CardEntity prefab: " + PrefabPath);
        }

        [MenuItem("CardGame/CardBattle/Create Default Battle Layout")]
        public static void CreateBattleLayoutMenu()
        {
            EnsureBattleLayoutAsset();
            AssetDatabase.SaveAssets();
            Debug.Log("[CardBattle] Battle layout: " + LayoutPath);
        }

        [MenuItem("CardGame/CardBattle/Ensure Board Zone Anchors")]
        public static void EnsureBoardZoneAnchorsMenu()
        {
            var board = GameObject.Find("BattleBoard");
            if (board == null)
            {
                Debug.LogError("[CardBattle] BattleBoard 오브젝트를 찾을 수 없습니다.");
                return;
            }

            var (playerZone, enemyZone) = EnsureBattleBoardHierarchy(board.transform);
            var camera = Camera.main;
            EnsureBoardZoneLayout(playerZone, isPlayerTeam: true);
            EnsureBoardZoneLayout(enemyZone, isPlayerTeam: false);
            RewireBoardPresenter(playerZone, enemyZone);
            EnsureBattleBoardView(board.transform, camera);
            EditorUtility.SetDirty(board);
            Debug.Log("[CardBattle] 보드 앵커 계층 연결 완료. 슬롯 위치·회전은 덮어쓰지 않습니다.");
        }

        [MenuItem("CardGame/CardBattle/Wire Default Decks To Scene")]
        public static void WireDefaultDecksToSceneMenu()
        {
            var bridge = Object.FindFirstObjectByType<BattleBridge>();
            if (bridge == null)
            {
                Debug.LogError("[CardBattle] 씬에 BattleBridge가 없습니다.");
                return;
            }

            var bridgeSo = new SerializedObject(bridge);
            WireDefaultDecks(bridgeSo);
            bridgeSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bridge);
            Debug.Log("[CardBattle] BattleBridge 기본 덱을 Resources CardDataAsset으로 연결했습니다.");
        }

        public static void WireDefaultDecks(SerializedObject bridgeSo)
        {
            const string cardFolder = "Assets/Resources/CardBattle/Cards";
            AssignDeckArray(bridgeSo, "defaultPlayerDeck", DefaultDeckCatalog.PlayerCardIds, cardFolder);
            AssignDeckArray(bridgeSo, "defaultEnemyDeck", DefaultDeckCatalog.EnemyCardIds, cardFolder);
        }

        private static void AssignDeckArray(
            SerializedObject bridgeSo,
            string propertyName,
            string[] cardIds,
            string cardFolder)
        {
            var array = bridgeSo.FindProperty(propertyName);
            array.arraySize = cardIds.Length;
            for (var i = 0; i < cardIds.Length; i++)
            {
                var path = $"{cardFolder}/{cardIds[i]}.asset";
                array.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<CardDataAsset>(path);
            }
        }

        public static (Transform playerZone, Transform enemyZone) EnsureBattleBoardHierarchy(Transform battleBoard)
        {
            var boardPlane = GetOrCreateChild(battleBoard, "BoardPlane");
            if (boardPlane.GetComponent<BattleBoardPlane>() == null)
            {
                boardPlane.gameObject.AddComponent<BattleBoardPlane>();
            }

            var playerZone = MigrateZoneRoot(boardPlane, "PlayerZone", "PlayerBoardRoot");
            var enemyZone = MigrateZoneRoot(boardPlane, "EnemyZone", "EnemyBoardRoot");
            playerZone.localRotation = Quaternion.identity;
            playerZone.localPosition = Vector3.zero;
            enemyZone.localRotation = Quaternion.identity;
            enemyZone.localPosition = Vector3.zero;

            return (playerZone, enemyZone);
        }

        private static Transform MigrateZoneRoot(Transform boardPlane, string zoneName, string legacyName)
        {
            var zone = boardPlane.Find(zoneName);
            if (zone != null)
            {
                return zone;
            }

            var battleBoard = boardPlane.parent;
            Transform legacy = null;
            if (battleBoard != null)
            {
                legacy = battleBoard.Find(legacyName);
            }

            if (legacy != null)
            {
                legacy.name = zoneName;
                legacy.SetParent(boardPlane, false);
                return legacy;
            }

            return GetOrCreateChild(boardPlane, zoneName);
        }

        private static void RewireBoardPresenter(Transform playerZone, Transform enemyZone)
        {
            var presenter = Object.FindFirstObjectByType<CardBoardPresenter>();
            if (presenter == null)
            {
                return;
            }

            var so = new SerializedObject(presenter);
            so.FindProperty("playerBoardRoot").objectReferenceValue = playerZone;
            so.FindProperty("enemyBoardRoot").objectReferenceValue = enemyZone;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
        }

        private static void EnsureBattleBoardView(Transform battleBoard, Camera camera)
        {
            EnsureBattleBoardViewComponent(battleBoard, camera);
        }

        public static void EnsureBattleBoardViewComponent(Transform battleBoard, Camera camera)
        {
            var view = battleBoard.GetComponent<BattleBoardView>();
            if (view == null)
            {
                view = battleBoard.gameObject.AddComponent<BattleBoardView>();
            }

            var so = new SerializedObject(view);
            so.FindProperty("battleCamera").objectReferenceValue = camera;
            so.FindProperty("boardPlane").objectReferenceValue = battleBoard.Find("BoardPlane");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        public static BattleBoardZoneLayout EnsureBoardZoneLayout(
            Transform zoneRoot,
            bool isPlayerTeam)
        {
            var zone = zoneRoot.GetComponent<BattleBoardZoneLayout>();
            if (zone == null)
            {
                zone = zoneRoot.gameObject.AddComponent<BattleBoardZoneLayout>();
            }

            var slotsRoot = GetOrCreateChild(zoneRoot, "BattlefieldSlots", out var slotsRootCreated);
            if (slotsRootCreated)
            {
                slotsRoot.localPosition = Vector3.zero;
                slotsRoot.localRotation = Quaternion.identity;
            }

            var slots = new Transform[BattleField.SlotCount];
            for (var i = 0; i < slots.Length; i++)
            {
                var slotName = $"Slot_{i}";
                var slot = GetOrCreateChild(slotsRoot, slotName, out var slotCreated);
                if (slotCreated)
                {
                    slot.localPosition = Vector3.zero;
                    slot.localRotation = Quaternion.identity;
                }

                slots[i] = slot;
            }

            var reserveRoot = GetOrCreateChild(zoneRoot, "Reserve", out var reserveRootCreated);
            if (reserveRootCreated)
            {
                reserveRoot.localPosition = Vector3.zero;
                reserveRoot.localRotation = Quaternion.identity;
            }

            var stackOrigin = GetOrCreateChild(reserveRoot, "StackOrigin", out var stackCreated);
            if (stackCreated)
            {
                stackOrigin.localPosition = Vector3.zero;
                stackOrigin.localRotation = Quaternion.identity;
            }

            zone.Configure(isPlayerTeam, slots, stackOrigin);
            EditorUtility.SetDirty(zone);
            return zone;
        }

        private static Transform GetOrCreateChild(Transform parent, string childName)
        {
            return GetOrCreateChild(parent, childName, out _);
        }

        private static Transform GetOrCreateChild(Transform parent, string childName, out bool created)
        {
            var existing = parent.Find(childName);
            if (existing != null)
            {
                created = false;
                return existing;
            }

            var child = new GameObject(childName).transform;
            child.SetParent(parent, false);
            created = true;
            return child;
        }

        public static CardEntity EnsureCardEntityPrefab()
        {
            var existingGo = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existingGo != null && !NeedsPrefabRebuild(existingGo))
            {
                return existingGo.GetComponent<CardEntity>();
            }

            return RebuildCardEntityPrefab();
        }

        private static CardEntity RebuildCardEntityPrefab()
        {
            var existingGo = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existingGo != null)
            {
                AssetDatabase.DeleteAsset(PrefabPath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? "Assets/Prefabs/CardBattle");

            var faceMaterial = EnsureCardFaceMaterial();
            if (faceMaterial == null)
            {
                return null;
            }

            var root = new GameObject("CardEntity");
            try
            {
                var shakeRoot = new GameObject("ShakeRoot");
                shakeRoot.transform.SetParent(root.transform, false);

                var collider = shakeRoot.AddComponent<BoxCollider>();
                collider.size = new Vector3(CardFaceView.DefaultWidth, CardFaceView.DefaultHeight, 0.08f);
                collider.center = Vector3.zero;

                var front = CreateCardFace(
                    "Front",
                    shakeRoot.transform,
                    new Vector3(0f, 0f, 0.012f),
                    Quaternion.identity,
                    faceMaterial,
                    sortingOrder: 1);
                var back = CreateCardFace(
                    "Back",
                    shakeRoot.transform,
                    new Vector3(0f, 0f, -0.012f),
                    Quaternion.Euler(0f, 180f, 0f),
                    faceMaterial,
                    sortingOrder: 0);

                var fallbackSprite = LoadFallbackSprite();
                front.ApplySprite(fallbackSprite);
                back.ApplySprite(fallbackSprite);

                var nameLabel = CreateWorldLabel("NameLabel", shakeRoot.transform, new Vector3(0f, 0.75f, 0.02f), 2.4f);
                var hpLabel = CreateWorldLabel("HpLabel", shakeRoot.transform, new Vector3(0f, -0.85f, 0.02f), 2f);

                var entity = root.AddComponent<CardEntity>();
                WireCardEntity(entity, shakeRoot.transform, front, back, nameLabel, hpLabel);

                var prefabRoot = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefabRoot == null)
                {
                    Debug.LogError("[CardBattle] CardEntity 프리팹 저장 실패: " + PrefabPath);
                    return null;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return prefabRoot.GetComponent<CardEntity>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        public static BattleLayoutConfig EnsureBattleLayoutAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BattleLayoutConfig>(LayoutPath);
            if (existing != null)
            {
                existing.cardEntityPrefab = EnsureCardEntityPrefab();
                EditorUtility.SetDirty(existing);
                return existing;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(LayoutPath) ?? "Assets/Resources/CardBattle");

            var layout = ScriptableObject.CreateInstance<BattleLayoutConfig>();
            layout.cardEntityPrefab = EnsureCardEntityPrefab();
            if (layout.cardEntityPrefab == null)
            {
                Debug.LogError("[CardBattle] BattleLayout 생성 중단 — CardEntity 프리팹이 없습니다.");
                return null;
            }

            AssetDatabase.CreateAsset(layout, LayoutPath);
            AssetDatabase.SaveAssets();
            return layout;
        }

        private static bool NeedsPrefabRebuild(GameObject root)
        {
            if (root.GetComponentInChildren<SpriteRenderer>(true) != null)
            {
                return true;
            }

            var entity = root.GetComponent<CardEntity>();
            if (entity == null)
            {
                return true;
            }

            if (root.GetComponent<BoxCollider>() != null)
            {
                return true;
            }

            var so = new SerializedObject(entity);
            return so.FindProperty("frontFace").objectReferenceValue == null
                || so.FindProperty("backFace").objectReferenceValue == null;
        }

        private static Material EnsureCardFaceMaterial()
        {
            var defaultPath = AssetDatabase.GUIDToAssetPath(DefaultSpriteMaterialGuid);
            var defaultMaterial = AssetDatabase.LoadAssetAtPath<Material>(defaultPath);
            if (defaultMaterial != null)
            {
                return defaultMaterial;
            }

            var existing = AssetDatabase.LoadAssetAtPath<Material>(CardFaceMaterialPath);
            if (existing != null)
            {
                return existing;
            }

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("[CardBattle] Sprites/Default 셰이더를 찾을 수 없습니다.");
                return null;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(CardFaceMaterialPath) ?? "Assets/Materials/CardBattle");

            var material = new Material(shader)
            {
                name = "CardFace_Default"
            };
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.renderQueue = (int)RenderQueue.Transparent;

            AssetDatabase.CreateAsset(material, CardFaceMaterialPath);
            return material;
        }

        private static CardFaceView CreateCardFace(
            string name,
            Transform parent,
            Vector3 localPos,
            Quaternion localRot,
            Material sharedMaterial,
            int sortingOrder = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = new Vector3(CardFaceView.DefaultWidth, CardFaceView.DefaultHeight, 1f);

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetBuiltinQuad();

            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = sharedMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            meshRenderer.sortingOrder = sortingOrder;

            return go.AddComponent<CardFaceView>();
        }

        private static Mesh GetBuiltinQuad()
        {
            if (builtinQuad == null)
            {
                builtinQuad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            }

            return builtinQuad;
        }

        private static TextMeshPro CreateWorldLabel(string name, Transform parent, Vector3 localPos, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(2f, 0.5f);
            tmp.text = name;
            tmp.sortingOrder = 2;
            return tmp;
        }

        private static void WireCardEntity(
            CardEntity entity,
            Transform shakeRoot,
            CardFaceView front,
            CardFaceView back,
            TextMeshPro nameLabel,
            TextMeshPro hpLabel)
        {
            var so = new SerializedObject(entity);
            so.FindProperty("shakeRoot").objectReferenceValue = shakeRoot;
            so.FindProperty("frontFace").objectReferenceValue = front;
            so.FindProperty("backFace").objectReferenceValue = back;
            so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("hpLabel").objectReferenceValue = hpLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite LoadFallbackSprite()
        {
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (sprite != null)
            {
                return sprite;
            }

            return Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        }
    }
}
