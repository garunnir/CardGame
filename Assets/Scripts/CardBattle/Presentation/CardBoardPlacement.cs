using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    internal static class CardBoardPlacement
    {
        internal readonly struct AnchorPlacement
        {
            public readonly Transform Parent;
            public readonly Vector3 LocalPosition;
            public readonly Quaternion LocalRotation;

            public AnchorPlacement(Transform parent, Vector3 localPosition, Quaternion localRotation)
            {
                Parent = parent;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
            }
        }

        public static bool HasZoneLayouts(Transform playerBoardRoot, Transform enemyBoardRoot)
        {
            return ResolveZoneLayout(playerBoardRoot) != null
                && ResolveZoneLayout(enemyBoardRoot) != null;
        }

        public static BattleBoardZoneLayout ResolveZoneLayout(Transform boardRoot)
        {
            if (boardRoot == null)
            {
                Debug.LogError("[CardBattle] BoardRoot가 없습니다.");
                return null;
            }

            var zone = boardRoot.GetComponent<BattleBoardZoneLayout>();
            if (zone == null)
            {
                Debug.LogError(
                    $"[CardBattle] {boardRoot.name}에 BattleBoardZoneLayout이 없습니다. "
                    + "CardGame/CardBattle/Ensure Board Zone Anchors로 전장 앵커를 연결하세요.");
                return null;
            }

            if (zone.BattlefieldCenter == null)
            {
                Debug.LogError($"[CardBattle] {boardRoot.name} 전장 중심 앵커가 비어 있습니다.");
                return null;
            }

            return zone;
        }

        public static AnchorPlacement ResolvePlacement(
            BattleBoardZoneLayout zone,
            CardModel model,
            CardModel[] battlefield,
            List<CardModel> reserve,
            Transform fallback)
        {
            if (IsOnBattlefield(model, battlefield)
                && zone.TryComputeBattlefieldPose(model, battlefield, out var localPosition, out var localRotation))
            {
                return new AnchorPlacement(
                    zone.BattlefieldCenter != null ? zone.BattlefieldCenter : fallback,
                    localPosition,
                    localRotation);
            }

            var stackIndex = reserve.IndexOf(model);
            if (stackIndex < 0)
            {
                stackIndex = 0;
            }

            return ResolveReserveStackPlacement(zone, stackIndex, fallback);
        }

        public static AnchorPlacement ResolveReserveStackPlacement(
            BattleBoardZoneLayout zone,
            int stackIndex,
            Transform fallback)
        {
            return new AnchorPlacement(
                zone.ReserveStackOrigin != null ? zone.ReserveStackOrigin : fallback,
                zone.GetReserveStackLocalOffset(stackIndex),
                Quaternion.identity);
        }

        private static bool IsOnBattlefield(CardModel model, CardModel[] battlefield)
        {
            for (var i = 0; i < battlefield.Length; i++)
            {
                if (battlefield[i] == model)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
