using System.Collections.Generic;
using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Core
{
    public static class SlotSupportAggregator
    {
        public static List<HeroSupportContribution> PlanContributions(CardModel[] battlefield)
        {
            var list = new List<HeroSupportContribution>();
            if (battlefield == null)
            {
                return list;
            }

            for (var i = 0; i < battlefield.Length; i++)
            {
                var card = battlefield[i];
                if (card == null || !card.IsAlive)
                {
                    continue;
                }

                var effect = HeroSupportLibrary.GetForCard(card);
                if (!effect.HasAnyEffect)
                {
                    continue;
                }

                list.Add(new HeroSupportContribution(card, effect));
            }

            return list;
        }

        public static HeroSupportTotals SumForStrike(IReadOnlyList<HeroSupportContribution> contributions)
        {
            return HeroSupportTotals.Sum(contributions);
        }
    }
}
