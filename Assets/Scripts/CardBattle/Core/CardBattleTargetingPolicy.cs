using System;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Input;

namespace CardGame.CardBattle.Core
{
    /// <summary>CardBattle 공격 타게팅 규칙을 공용 드래그 정책으로 캡슐화.</summary>
    public sealed class CardBattleTargetingPolicy : IDragTargetingPolicy<CardModel, CardModel, BattleActionRequest>
    {
        private readonly Func<CardModel, bool> canAcceptTarget;

        public CardBattleTargetingPolicy(Func<CardModel, bool> canAcceptTarget = null)
        {
            this.canAcceptTarget = canAcceptTarget ?? (_ => true);
        }

        public bool CanStartDrag(CardModel source)
        {
            return source != null
                && source.IsAlive
                && source.IsPlayerTeam
                && canAcceptTarget(source);
        }

        public bool IsValidHover(CardModel source, CardModel hoverTarget)
        {
            if (!CanStartDrag(source))
            {
                return false;
            }

            return hoverTarget != null
                && hoverTarget.IsAlive
                && hoverTarget.IsPlayerTeam != source.IsPlayerTeam
                && canAcceptTarget(hoverTarget);
        }

        public bool TryBuildAction(CardModel source, CardModel dropTarget, out BattleActionRequest action)
        {
            if (IsValidHover(source, dropTarget))
            {
                action = new BattleActionRequest(source, dropTarget);
                return true;
            }

            action = default;
            return false;
        }
    }
}
