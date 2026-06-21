using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Presentation
{
    public enum TurnStartStatKind
    {
        Heal,
        MpGain,
    }

    public readonly struct TurnStartEffectEvent
    {
        public TurnStartEffectEvent(
            CardModel source,
            CardModel targetCard,
            HeroModel targetHero,
            TurnStartStatKind kind,
            int fromValue,
            int toValue)
        {
            Source = source;
            TargetCard = targetCard;
            TargetHero = targetHero;
            Kind = kind;
            FromValue = fromValue;
            ToValue = toValue;
        }

        public CardModel Source { get; }
        public CardModel TargetCard { get; }
        public HeroModel TargetHero { get; }
        public TurnStartStatKind Kind { get; }
        public int FromValue { get; }
        public int ToValue { get; }
    }

    public static class TurnStartEffectAggregator
    {
        public static List<TurnStartEffectEvent> Plan(
            CardModel[] playerBattlefield,
            CardModel[] enemyBattlefield,
            HeroArenaField heroArena,
            bool isPlayerTurn)
        {
            var battlefield = isPlayerTurn ? playerBattlefield : enemyBattlefield;
            var events = new List<TurnStartEffectEvent>();
            AppendCardHealEvents(events, TurnStartHealEffect.Plan(battlefield));

            if (heroArena != null)
            {
                var bridge = new BattleEffectBridge();
                AppendHeroSupportEvents(
                    events,
                    bridge.PlanTurnStart(heroArena, playerBattlefield, enemyBattlefield, isPlayerTurn));
            }

            return events;
        }

        public static IReadOnlyList<TurnStartEffectEvent> FromCardHealEvents(IReadOnlyList<TurnStartHealEvent> healEvents)
        {
            var events = new List<TurnStartEffectEvent>();
            AppendCardHealEvents(events, healEvents);
            return events;
        }

        public static IReadOnlyList<TurnStartEffectEvent> FromHeroSupportEvents(
            IReadOnlyList<HeroSupportHealEvent> heroEvents)
        {
            var events = new List<TurnStartEffectEvent>();
            AppendHeroSupportEvents(events, heroEvents);
            return events;
        }


        private static void AppendCardHealEvents(
            List<TurnStartEffectEvent> events,
            IReadOnlyList<TurnStartHealEvent> healEvents)
        {
            if (healEvents == null)
            {
                return;
            }

            for (var i = 0; i < healEvents.Count; i++)
            {
                var healEvent = healEvents[i];
                if (healEvent.Healer == null || healEvent.Target == null)
                {
                    continue;
                }

                events.Add(new TurnStartEffectEvent(
                    healEvent.Healer,
                    healEvent.Target,
                    null,
                    TurnStartStatKind.Heal,
                    healEvent.FromHp,
                    healEvent.ToHp));
            }
        }

        private static void AppendHeroSupportEvents(
            List<TurnStartEffectEvent> events,
            IReadOnlyList<HeroSupportHealEvent> heroEvents)
        {
            if (heroEvents == null)
            {
                return;
            }

            for (var i = 0; i < heroEvents.Count; i++)
            {
                var heroEvent = heroEvents[i];
                if (heroEvent.Hero == null)
                {
                    continue;
                }

                if (heroEvent.SourceCard == null)
                {
                    if (!heroEvent.IsMpGain)
                    {
                        continue;
                    }

                    events.Add(new TurnStartEffectEvent(
                        null,
                        null,
                        heroEvent.Hero,
                        TurnStartStatKind.MpGain,
                        heroEvent.FromMp,
                        heroEvent.ToMp));
                    continue;
                }

                if (heroEvent.IsMpGain)
                {
                    events.Add(new TurnStartEffectEvent(
                        heroEvent.SourceCard,
                        null,
                        heroEvent.Hero,
                        TurnStartStatKind.MpGain,
                        heroEvent.FromMp,
                        heroEvent.ToMp));
                }
                else
                {
                    events.Add(new TurnStartEffectEvent(
                        heroEvent.SourceCard,
                        null,
                        heroEvent.Hero,
                        TurnStartStatKind.Heal,
                        heroEvent.FromHp,
                        heroEvent.ToHp));
                }
            }
        }
    }
}
