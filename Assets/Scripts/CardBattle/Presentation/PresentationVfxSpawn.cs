using CardGame.CardBattle.Cards;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    internal static class PresentationVfxSpawn
    {
        public static GameObject InstantiateAt(GameObject prefab, Transform anchor)
        {
            if (prefab == null || anchor == null)
            {
                return null;
            }

            var spawnPosition = ResolveSpawnPosition(anchor);
            var instance = Object.Instantiate(prefab, spawnPosition, anchor.rotation);
            ApplySortingOrder(instance, ResolveSortingOrder(anchor));
            return instance;
        }

        private static Vector3 ResolveSpawnPosition(Transform anchor)
        {
            var offsetLocal = anchor.GetComponent<HeroEntity>() != null
                ? HeroVisualSorting.BattleVfxOffsetLocal
                : CardFaceView.BattleVfxOffsetLocal;

            return anchor.position
                + anchor.TransformVector(offsetLocal)
                + anchor.forward * CardFaceView.BattleVfxFaceForwardOffset;
        }

        private static int ResolveSortingOrder(Transform anchor)
        {
            if (anchor.GetComponent<HeroEntity>() != null)
            {
                return HeroVisualSorting.BattleVfx;
            }

            return CardFaceView.BattleVfxSortingOrder;
        }

        private static void ApplySortingOrder(GameObject root, int order)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingOrder = order;
            }
        }
    }
}
