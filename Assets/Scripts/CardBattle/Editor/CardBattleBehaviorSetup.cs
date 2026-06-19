using System.IO;
using CardGame.CardBattle.Cards;
using UnityEditor;
using UnityEngine;

namespace CardGame.CardBattle.Editor
{
    public static class CardBattleBehaviorSetup
    {
        private const string BehaviorFolder = "Assets/Resources/CardBattle/Behaviors";
        private const string CardFolder = "Assets/Resources/CardBattle/Cards";
        private const string IllustrationPath = "Assets/Resources/CardBattle/Art/CardIllustration_Default.png";
        private const string VfxFolder = "Assets/Resources/CardBattle/Vfx";
        private const string SharedHitVfxAssetPath = "Assets/Resources/CardBattle/Vfx/SharedHitVfx.prefab";
        private const string DefaultOnHitVfxPath =
            "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit A (Red).prefab";
        private const string DefaultProjectileVfxPath =
            "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Electric/CFXR3 Hit Electric C (Air).prefab";

        [MenuItem("CardGame/CardBattle/Create Default Behavior Assets")]
        public static void CreateDefaultBehaviorAssets()
        {
            EnsureFolder(BehaviorFolder);

            CreateOrUpdateBehavior<NormalBehaviorAsset>("Behavior_Normal", CardType.Normal);
            CreateOrUpdateBehavior<RangedBehaviorAsset>("Behavior_Ranged", CardType.Ranged);
            CreateOrUpdateBehavior<MusouBehaviorAsset>("Behavior_Musou", CardType.Musou);
            CreateOrUpdateBehavior<HealerBehaviorAsset>("Behavior_Healer", CardType.Healer);

            AssignDefaultPresentationVfx();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CardBattle] 타입별 CardBehaviorAsset 4종 생성 완료.");
        }

        [MenuItem("CardGame/CardBattle/Assign Default Presentation Vfx")]
        public static void AssignDefaultPresentationVfx()
        {
            EnsureFolder(VfxFolder);
            var sharedHit = EnsureSharedHitVfxPrefab();
            var onHit = LoadPrefab(DefaultOnHitVfxPath);
            var projectile = LoadPrefab(DefaultProjectileVfxPath);
            if (onHit == null)
            {
                Debug.LogWarning("[CardBattle] 기본 명중 VFX 프리팹을 찾을 수 없습니다: " + DefaultOnHitVfxPath);
            }

            AssignOnHitAndReceivedVfx(LoadBehavior<NormalBehaviorAsset>("Behavior_Normal"), onHit, sharedHit);
            AssignOnHitAndReceivedVfx(LoadBehavior<RangedBehaviorAsset>("Behavior_Ranged"), onHit, sharedHit);
            if (LoadBehavior<RangedBehaviorAsset>("Behavior_Ranged") is { } ranged && projectile != null)
            {
                ranged.presentation.projectileVfxPrefab = projectile;
                EditorUtility.SetDirty(ranged);
            }

            var musou = LoadBehavior<MusouBehaviorAsset>("Behavior_Musou");
            AssignOnHitAndReceivedVfx(musou, onHit, sharedHit);
            if (musou != null && onHit != null)
            {
                musou.presentation.secondaryHitVfxPrefab = onHit;
                EditorUtility.SetDirty(musou);
            }

            AssignOnHitAndReceivedVfx(LoadBehavior<HealerBehaviorAsset>("Behavior_Healer"), onHit, sharedHit);

            AssetDatabase.SaveAssets();
            Debug.Log("[CardBattle] 명중·피격 VFX 기본 할당 완료.");
        }

        [MenuItem("CardGame/CardBattle/Assign Default Card Illustrations")]
        public static void AssignDefaultCardIllustrations()
        {
            var placeholder = AssetDatabase.LoadAssetAtPath<Sprite>(IllustrationPath);
            if (placeholder == null)
            {
                Debug.LogError("[CardBattle] 기본 일러스트 없음: " + IllustrationPath);
                return;
            }

            var guids = AssetDatabase.FindAssets("t:CardDataAsset", new[] { CardFolder });
            var updated = 0;
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var card = AssetDatabase.LoadAssetAtPath<CardDataAsset>(path);
                if (card == null || card.illustration != null)
                {
                    continue;
                }

                card.illustration = placeholder;
                EditorUtility.SetDirty(card);
                updated++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[CardBattle] CardDataAsset 일러스트 {updated}건에 기본 스프라이트를 지정했습니다.");
        }

        [MenuItem("CardGame/CardBattle/Link Card Data To Behaviors")]
        public static void LinkCardDataToBehaviors()
        {
            var normal = LoadBehavior<NormalBehaviorAsset>("Behavior_Normal");
            var ranged = LoadBehavior<RangedBehaviorAsset>("Behavior_Ranged");
            var musou = LoadBehavior<MusouBehaviorAsset>("Behavior_Musou");
            var healer = LoadBehavior<HealerBehaviorAsset>("Behavior_Healer");

            if (normal == null || ranged == null || musou == null || healer == null)
            {
                Debug.LogWarning("[CardBattle] Behavior asset 없음 — Create Default Behavior Assets 먼저 실행.");
                CreateDefaultBehaviorAssets();
                normal = LoadBehavior<NormalBehaviorAsset>("Behavior_Normal");
                ranged = LoadBehavior<RangedBehaviorAsset>("Behavior_Ranged");
                musou = LoadBehavior<MusouBehaviorAsset>("Behavior_Musou");
                healer = LoadBehavior<HealerBehaviorAsset>("Behavior_Healer");
            }

            LinkCardsInFolder(CardFolder, normal, ranged, musou, healer);
            AssetDatabase.SaveAssets();
            Debug.Log("[CardBattle] CardDataAsset → CardBehaviorAsset 연결 완료.");
        }

        private static void LinkCardsInFolder(
            string folder,
            NormalBehaviorAsset normal,
            RangedBehaviorAsset ranged,
            MusouBehaviorAsset musou,
            HealerBehaviorAsset healer)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var guids = AssetDatabase.FindAssets("t:CardDataAsset", new[] { folder });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var card = AssetDatabase.LoadAssetAtPath<CardDataAsset>(path);
                if (card == null)
                {
                    continue;
                }

                card.behavior = ResolveBehaviorForCard(card, normal, ranged, musou, healer);
                EditorUtility.SetDirty(card);
            }
        }

        private static CardBehaviorAsset ResolveBehaviorForCard(
            CardDataAsset card,
            NormalBehaviorAsset normal,
            RangedBehaviorAsset ranged,
            MusouBehaviorAsset musou,
            HealerBehaviorAsset healer)
        {
            var id = card.cardId ?? string.Empty;
            var name = card.displayName ?? string.Empty;
            var key = (id + " " + name).ToLowerInvariant();

            if (key.Contains("ranged"))
            {
                return ranged;
            }

            if (key.Contains("musou"))
            {
                return musou;
            }

            if (key.Contains("healer"))
            {
                return healer;
            }

            return normal;
        }

        private static void CreateOrUpdateBehavior<T>(string assetName, CardType type)
            where T : CardBehaviorAsset
        {
            var path = $"{BehaviorFolder}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.behaviorId = assetName.ToLowerInvariant();
            EnsurePresentation(asset);
            EditorUtility.SetDirty(asset);
        }

        private static void EnsurePresentation(CardBehaviorAsset asset)
        {
            switch (asset)
            {
                case NormalBehaviorAsset normal when normal.presentation == null:
                    normal.presentation = new NormalBehaviorPresentation();
                    break;
                case RangedBehaviorAsset ranged when ranged.presentation == null:
                    ranged.presentation = new RangedBehaviorPresentation();
                    break;
                case MusouBehaviorAsset musou when musou.presentation == null:
                    musou.presentation = new MusouBehaviorPresentation();
                    break;
                case HealerBehaviorAsset healer when healer.presentation == null:
                    healer.presentation = new HealerBehaviorPresentation();
                    break;
            }
        }

        private static GameObject LoadPrefab(string assetPath)
        {
            return string.IsNullOrEmpty(assetPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }

        private static GameObject EnsureSharedHitVfxPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(SharedHitVfxAssetPath);
            if (existing != null)
            {
                return existing;
            }

            var source = LoadPrefab(DefaultOnHitVfxPath);
            if (source == null)
            {
                return null;
            }

            var instance = UnityEngine.Object.Instantiate(source);
            var created = PrefabUtility.SaveAsPrefabAsset(instance, SharedHitVfxAssetPath);
            UnityEngine.Object.DestroyImmediate(instance);
            return created;
        }

        private static void AssignOnHitAndReceivedVfx(
            CardBehaviorAsset behavior,
            GameObject onHitPrefab,
            GameObject receivedHitPrefab)
        {
            if (behavior == null)
            {
                return;
            }

            EnsurePresentation(behavior);
            switch (behavior)
            {
                case NormalBehaviorAsset normal:
                    if (onHitPrefab != null)
                    {
                        normal.presentation.hitVfxPrefab = onHitPrefab;
                        normal.presentation.deathVfxPrefab = onHitPrefab;
                    }

                    if (receivedHitPrefab != null)
                    {
                        normal.presentation.receivedHitVfxPrefab = receivedHitPrefab;
                    }

                    EditorUtility.SetDirty(normal);
                    break;
                case RangedBehaviorAsset ranged:
                    if (onHitPrefab != null)
                    {
                        ranged.presentation.hitVfxPrefab = onHitPrefab;
                        ranged.presentation.deathVfxPrefab = onHitPrefab;
                    }

                    if (receivedHitPrefab != null)
                    {
                        ranged.presentation.receivedHitVfxPrefab = receivedHitPrefab;
                    }

                    EditorUtility.SetDirty(ranged);
                    break;
                case MusouBehaviorAsset musou:
                    if (onHitPrefab != null)
                    {
                        musou.presentation.hitVfxPrefab = onHitPrefab;
                        musou.presentation.deathVfxPrefab = onHitPrefab;
                    }

                    if (receivedHitPrefab != null)
                    {
                        musou.presentation.receivedHitVfxPrefab = receivedHitPrefab;
                    }

                    EditorUtility.SetDirty(musou);
                    break;
                case HealerBehaviorAsset healer:
                    if (onHitPrefab != null)
                    {
                        healer.presentation.hitVfxPrefab = onHitPrefab;
                        healer.presentation.deathVfxPrefab = onHitPrefab;
                    }

                    if (receivedHitPrefab != null)
                    {
                        healer.presentation.receivedHitVfxPrefab = receivedHitPrefab;
                    }

                    EditorUtility.SetDirty(healer);
                    break;
            }
        }

        private static T LoadBehavior<T>(string assetName) where T : CardBehaviorAsset
        {
            return AssetDatabase.LoadAssetAtPath<T>($"{BehaviorFolder}/{assetName}.asset");
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            var leaf = Path.GetFileName(folderPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
