using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;

namespace CardGame.CardBattle.Presentation
{
    internal static class CardBoardDeployer
    {
        public static async UniTask PlaceBattlefieldCardAsync(
            BattleField field,
            CardModel model,
            CardEntity entity,
            CardBoardPlacement.AnchorPlacement placement,
            bool isNewToBattlefield,
            bool animate)
        {
            var isDeployAnimate = isNewToBattlefield && animate;

            if (isDeployAnimate)
            {
                field?.MarkPendingBattlefieldDeploy(model);
                CardBoardInputTargeting.Refresh(field, model, entity);
            }

            try
            {
                ICardBoardMotion motion = entity;
                await motion.ApplyPlacement(
                    placement.Parent,
                    placement.LocalPosition,
                    placement.LocalRotation,
                    CardBoardPhase.BattlefieldFaceUp,
                    animate);
            }
            finally
            {
                if (isDeployAnimate)
                {
                    field?.ClearPendingBattlefieldDeploy(model);
                }

                CardBoardInputTargeting.Refresh(field, model, entity);
            }
        }

        public static async UniTask PlaceReserveCardAsync(
            BattleField field,
            CardModel model,
            CardEntity entity,
            CardBoardPlacement.AnchorPlacement placement,
            bool animate)
        {
            ICardBoardMotion motion = entity;
            await motion.ApplyPlacement(
                placement.Parent,
                placement.LocalPosition,
                placement.LocalRotation,
                CardBoardPhase.ReserveFaceDown,
                animate);
            CardBoardInputTargeting.Refresh(field, model, entity);
        }
    }
}
