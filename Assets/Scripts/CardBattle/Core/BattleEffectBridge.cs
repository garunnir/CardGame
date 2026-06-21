using System.Collections.Generic;
using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Core
{
    public sealed class BattleEffectBridge
    {
        public IReadOnlyList<HeroSupportHealEvent> PlanTurnStart(
            HeroArenaField heroArena,
            CardModel[] playerBattlefield,
            CardModel[] enemyBattlefield,
            bool isPlayerTurn)
        {
            var events = new List<HeroSupportHealEvent>();
            if (heroArena == null)
            {
                return events;
            }

            var battlefield = isPlayerTurn ? playerBattlefield : enemyBattlefield;
            var hero = heroArena.GetHero(isPlayerTurn);
            if (hero == null || !hero.IsAlive)
            {
                return events;
            }

            var contributions = SlotSupportAggregator.PlanContributions(battlefield);
            for (var i = 0; i < contributions.Count; i++)
            {
                var contribution = contributions[i];
                var effect = contribution.Effect;

                if (effect.turnStartHeroHeal > 0)
                {
                    var fromHp = hero.CurrentHp;
                    hero.Heal(effect.turnStartHeroHeal);
                    events.Add(new HeroSupportHealEvent(
                        contribution.SourceCard,
                        hero,
                        effect.turnStartHeroHeal,
                        fromHp,
                        hero.CurrentHp,
                        hero.CurrentMp,
                        hero.CurrentMp,
                        isMpGain: false));
                }

                if (effect.mpGainOnTurnStart > 0)
                {
                    var fromMp = hero.CurrentMp;
                    hero.AddMp(effect.mpGainOnTurnStart);
                    events.Add(new HeroSupportHealEvent(
                        contribution.SourceCard,
                        hero,
                        effect.mpGainOnTurnStart,
                        hero.CurrentHp,
                        hero.CurrentHp,
                        fromMp,
                        hero.CurrentMp,
                        isMpGain: true));
                }
            }

            return events;
        }

        public static void ApplyTurnStart(IReadOnlyList<HeroSupportHealEvent> events)
        {
            // 도메인은 PlanTurnStart에서 이미 적용됨. 연출·UI는 GameManager/Presenter가 처리.
        }
    }
}
