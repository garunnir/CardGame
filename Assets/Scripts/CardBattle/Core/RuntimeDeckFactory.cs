using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using UnityEngine;

namespace CardGame.CardBattle.Core
{
    /// <summary>CardDataAsset 미배치 시 런타임 테스트 덱 생성.</summary>
    public static class RuntimeDeckFactory
    {
        private static readonly CardType[] DefaultPattern =
        {
            CardType.Normal,
            CardType.Ranged,
            CardType.Musou,
            CardType.Healer,
            CardType.Normal,
            CardType.Ranged
        };

        public static List<CardDataAsset> CreateDefaultDeck(string teamPrefix)
        {
            var list = new List<CardDataAsset>(6);
            for (var i = 0; i < DefaultPattern.Length; i++)
            {
                var type = DefaultPattern[i];
                var asset = ScriptableObject.CreateInstance<CardDataAsset>();
                asset.cardId = $"{teamPrefix}_{type}_{i}";
                asset.displayName = $"{teamPrefix} {type}";
                asset.behavior = CardBehaviorLibrary.GetRuntimeDefault(type);
                asset.maxHp = type == CardType.Musou ? 4 : 5;
                asset.heroSupport = HeroSupportLibrary.GetDefaultForType(type);
                list.Add(asset);
            }

            return list;
        }

        public static HeroDataAsset CreateDefaultHero(string teamPrefix)
        {
            var hero = ScriptableObject.CreateInstance<HeroDataAsset>();
            hero.heroId = $"{teamPrefix}_hero";
            hero.displayName = $"{teamPrefix} Commander";
            hero.maxHp = 20;
            hero.baseAttack = 4;
            hero.maxMp = 100;
            hero.mpGainPerTurn = 12;

            var normalAttack = ScriptableObject.CreateInstance<HeroNormalAttackBehaviorAsset>();
            normalAttack.behaviorId = $"{teamPrefix}_hero_normal";
            normalAttack.displayName = "평타";
            normalAttack.counterDamageOverride = 0;

            var shield = ScriptableObject.CreateInstance<HeroShieldBehaviorAsset>();
            shield.behaviorId = $"{teamPrefix}_hero_shield";
            shield.displayName = "보호막";
            shield.shieldAmount = 5;

            hero.normalAttackBehavior = normalAttack;
            hero.shieldBehavior = shield;
            return hero;
        }

        public static bool IsDeckValid(IList<CardDataAsset> deck)
        {
            if (deck == null || deck.Count < BattleField.SlotCount * 2)
            {
                return false;
            }

            for (var i = 0; i < deck.Count; i++)
            {
                if (deck[i] == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
