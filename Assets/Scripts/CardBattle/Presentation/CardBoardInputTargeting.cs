using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Presentation
{
    internal static class CardBoardInputTargeting
    {
        public static void Refresh(BattleField field, CardModel model, CardEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            entity.ApplyInputTargeting(
                CardTargetingRules.CanBeginPlayerDrag(field, model),
                CardTargetingRules.CanAcceptBattlefieldTarget(field, model));
        }

        public static void RefreshAll(BattleField field, CardBoardEntityRegistry registry)
        {
            foreach (var pair in registry.Entries)
            {
                Refresh(field, pair.Value.Model, pair.Value.Entity);
            }
        }

        public static void SetPhase(BattleField field, CardModel model, CardEntity entity, CardBoardPhase phase)
        {
            entity.SetPhase(phase);
            Refresh(field, model, entity);
        }
    }
}
