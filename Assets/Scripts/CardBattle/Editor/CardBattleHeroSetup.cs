using System.IO;
using CardGame.CardBattle.Bridge;
using CardGame.CardBattle.Cards;
using UnityEditor;
using UnityEngine;

namespace CardGame.CardBattle.Editor
{
    public static class CardBattleHeroSetup
    {
        private const string HeroBehaviorFolder = "Assets/Resources/CardBattle/HeroBehaviors";
        private const string HeroDataFolder = "Assets/Resources/CardBattle/Heroes";
        private const string CardFolder = "Assets/Resources/CardBattle/Cards";

        [MenuItem("CardGame/CardBattle/Create Default Hero Assets")]
        public static void CreateDefaultHeroAssets()
        {
            EnsureFolder(HeroBehaviorFolder);
            EnsureFolder(HeroDataFolder);

            var normalAttack = CreateOrUpdateBehavior<HeroNormalAttackBehaviorAsset>(
                "HeroBehavior_NormalAttack",
                "평타",
                "영웅 기본 공격");
            var shield = CreateOrUpdateBehavior<HeroShieldBehaviorAsset>(
                "HeroBehavior_Shield",
                "보호막",
                "MP 만땅 시 보호막 합산");

            CreateOrUpdateHero("HeroData_Player", "Player Commander", normalAttack, shield);
            CreateOrUpdateHero("HeroData_Enemy", "Enemy Commander", normalAttack, shield);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CardBattle] HeroDataAsset / HeroBehaviorAsset 기본 에셋 생성 완료.");
        }

        [MenuItem("CardGame/CardBattle/Assign Default Hero Support To Cards")]
        public static void AssignDefaultHeroSupportToCards()
        {
            if (!AssetDatabase.IsValidFolder(CardFolder))
            {
                Debug.LogWarning("[CardBattle] CardData 폴더 없음.");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:CardDataAsset", new[] { CardFolder });
            var updated = 0;
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var card = AssetDatabase.LoadAssetAtPath<CardDataAsset>(path);
                if (card == null || card.behavior == null)
                {
                    continue;
                }

                if (card.heroSupport.HasAnyEffect)
                {
                    continue;
                }

                card.heroSupport = HeroSupportLibrary.GetDefaultForType(card.behavior.StrategyType);
                EditorUtility.SetDirty(card);
                updated++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[CardBattle] CardDataAsset heroSupport {updated}건 할당 완료.");
        }

        public static void WireDefaultHeroes(SerializedObject bridgeSo)
        {
            var player = LoadHero("HeroData_Player");
            var enemy = LoadHero("HeroData_Enemy");
            bridgeSo.FindProperty("defaultPlayerHero").objectReferenceValue = player;
            bridgeSo.FindProperty("defaultEnemyHero").objectReferenceValue = enemy;
        }

        private static T CreateOrUpdateBehavior<T>(string assetName, string displayName, string description)
            where T : HeroBehaviorAsset
        {
            var path = $"{HeroBehaviorFolder}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.behaviorId = assetName.ToLowerInvariant();
            asset.displayName = displayName;
            asset.description = description;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void CreateOrUpdateHero(
            string assetName,
            string displayName,
            HeroNormalAttackBehaviorAsset normalAttack,
            HeroShieldBehaviorAsset shield)
        {
            var path = $"{HeroDataFolder}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<HeroDataAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<HeroDataAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.heroId = assetName.ToLowerInvariant();
            asset.displayName = displayName;
            asset.maxHp = 20;
            asset.baseAttack = 4;
            asset.maxMp = 100;
            asset.normalAttackBehavior = normalAttack;
            asset.shieldBehavior = shield;
            EditorUtility.SetDirty(asset);
        }

        private static HeroDataAsset LoadHero(string assetName)
        {
            return AssetDatabase.LoadAssetAtPath<HeroDataAsset>($"{HeroDataFolder}/{assetName}.asset");
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
