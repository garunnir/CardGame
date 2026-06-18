using CardGame.CardBattle.Cards;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>앵커 right/forward 기준 평면 와이어.</summary>
    public static class BattleBoardGizmos
    {
        public static void DrawFlatCardRect(Transform anchor)
        {
            if (anchor == null)
            {
                return;
            }

            DrawFlatCardRect(anchor, anchor.position);
        }

        public static void DrawFlatCardRect(Transform orientation, Vector3 worldCenter)
        {
            DrawFlatRect(
                orientation,
                worldCenter,
                CardFaceView.DefaultWidth,
                CardFaceView.DefaultHeight);
        }

        public static void DrawFlatRect(
            Transform orientation,
            Vector3 worldCenter,
            float width,
            float depth)
        {
            if (orientation == null)
            {
                return;
            }

            var halfW = width * 0.5f;
            var halfD = depth * 0.5f;
            var right = orientation.right * halfW;
            var forward = orientation.forward * halfD;

            var p0 = worldCenter - right - forward;
            var p1 = worldCenter + right - forward;
            var p2 = worldCenter + right + forward;
            var p3 = worldCenter - right + forward;

            Gizmos.DrawLine(p0, p1);
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p0);
        }

        public static void DrawFlatGrid(
            Transform orientation,
            Vector3 worldCenter,
            float width,
            float depth,
            int divisions)
        {
            if (orientation == null || divisions < 2)
            {
                return;
            }

            var halfW = width * 0.5f;
            var halfD = depth * 0.5f;
            var right = orientation.right * halfW;
            var forward = orientation.forward * halfD;

            var p0 = worldCenter - right - forward;
            var p1 = worldCenter + right - forward;
            var p2 = worldCenter + right + forward;
            var p3 = worldCenter - right + forward;

            for (var i = 1; i < divisions; i++)
            {
                var t = i / (float)divisions;
                Gizmos.DrawLine(Vector3.Lerp(p0, p1, t), Vector3.Lerp(p3, p2, t));
                Gizmos.DrawLine(Vector3.Lerp(p0, p3, t), Vector3.Lerp(p1, p2, t));
            }
        }
    }
}
