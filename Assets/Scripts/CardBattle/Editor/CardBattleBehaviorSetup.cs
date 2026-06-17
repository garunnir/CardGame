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

        [MenuItem("CardGame/CardBattle/Create Default Behavior Assets")]
        public static void CreateDefaultBehaviorAssets()
        {
            EnsureFolder(BehaviorFolder);

            CreateOrUpdateBehavior<NormalBehaviorAsset>("Behavior_Normal", CardType.Normal);
            CreateOrUpdateBehavior<RangedBehaviorAsset>("Behavior_Ranged", CardType.Ranged);
            CreateOrUpdateBehavior<MusouBehaviorAsset>("Behavior_Musou", CardType.Musou);
            CreateOrUpdateBehavior<HealerBehaviorAsset>("Behavior_Healer", CardType.Healer);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CardBattle] 타입별 CardBehaviorAsset 4종 생성 완료.");
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
