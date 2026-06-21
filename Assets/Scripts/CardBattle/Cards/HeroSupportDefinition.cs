using System;
using System.Collections.Generic;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Cards
{
    [Serializable]
    public struct HeroSupportDefinition
    {
        public int strikeBonus;
        public int turnStartHeroHeal;
        public int mpGainOnTurnStart;
        public int mpGainOnHeroStrike;
        public int defenseBonus;

        public bool HasAnyEffect =>
            strikeBonus != 0
            || turnStartHeroHeal != 0
            || mpGainOnTurnStart != 0
            || mpGainOnHeroStrike != 0
            || defenseBonus != 0;
    }

    public readonly struct HeroSupportContribution
    {
        public HeroSupportContribution(CardModel sourceCard, HeroSupportDefinition effect)
        {
            SourceCard = sourceCard;
            Effect = effect;
        }

        public CardModel SourceCard { get; }
        public HeroSupportDefinition Effect { get; }
    }

    public readonly struct HeroSupportTotals
    {
        public HeroSupportTotals(
            int strikeBonus,
            int mpGainOnHeroStrike,
            int defenseBonus)
        {
            StrikeBonus = strikeBonus;
            MpGainOnHeroStrike = mpGainOnHeroStrike;
            DefenseBonus = defenseBonus;
        }

        public int StrikeBonus { get; }
        public int MpGainOnHeroStrike { get; }
        public int DefenseBonus { get; }

        public static HeroSupportTotals Sum(IReadOnlyList<HeroSupportContribution> contributions)
        {
            var strike = 0;
            var mpStrike = 0;
            var defense = 0;
            if (contributions == null)
            {
                return new HeroSupportTotals(strike, mpStrike, defense);
            }

            for (var i = 0; i < contributions.Count; i++)
            {
                var effect = contributions[i].Effect;
                strike += effect.strikeBonus;
                mpStrike += effect.mpGainOnHeroStrike;
                defense += effect.defenseBonus;
            }

            return new HeroSupportTotals(strike, mpStrike, defense);
        }
    }

    public readonly struct HeroSupportHealEvent
    {
        public HeroSupportHealEvent(
            CardModel sourceCard,
            HeroModel hero,
            int amount,
            int fromHp,
            int toHp,
            int fromMp,
            int toMp,
            bool isMpGain)
        {
            SourceCard = sourceCard;
            Hero = hero;
            Amount = amount;
            FromHp = fromHp;
            ToHp = toHp;
            FromMp = fromMp;
            ToMp = toMp;
            IsMpGain = isMpGain;
        }

        public CardModel SourceCard { get; }
        public HeroModel Hero { get; }
        public int Amount { get; }
        public int FromHp { get; }
        public int ToHp { get; }
        public int FromMp { get; }
        public int ToMp { get; }
        public bool IsMpGain { get; }
    }

    public static class HeroSupportLibrary
    {
        public static HeroSupportDefinition GetForCard(CardModel card)
        {
            if (card?.Data == null)
            {
                return default;
            }

            if (card.Data.heroSupport.HasAnyEffect)
            {
                return card.Data.heroSupport;
            }

            return GetDefaultForType(card.CardType);
        }

        public static HeroSupportDefinition GetDefaultForType(CardType type)
        {
            switch (type)
            {
                case CardType.Ranged:
                    return new HeroSupportDefinition
                    {
                        mpGainOnTurnStart = 22,
                        mpGainOnHeroStrike = 8
                    };
                case CardType.Healer:
                    return new HeroSupportDefinition
                    {
                        turnStartHeroHeal = 1,
                        mpGainOnTurnStart = 10,
                        defenseBonus = 2
                    };
                case CardType.Musou:
                    return new HeroSupportDefinition
                    {
                        strikeBonus = 2,
                        mpGainOnHeroStrike = 14
                    };
                default:
                    return new HeroSupportDefinition
                    {
                        strikeBonus = 1,
                        mpGainOnHeroStrike = 8,
                        defenseBonus = 1
                    };
            }
        }
    }
}
