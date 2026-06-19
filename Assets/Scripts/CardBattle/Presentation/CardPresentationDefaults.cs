using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>연출 VFX 미할당 시 Resources 폴백.</summary>
    public static class CardPresentationDefaults
    {
        private const string SharedHitVfxResourcePath = "CardBattle/Vfx/SharedHitVfx";

        private static GameObject sharedHitVfxPrefab;

        public static GameObject ResolveOnHitVfx(GameObject preferred)
        {
            return ResolveShared(preferred);
        }

        public static GameObject ResolveReceivedHitVfx(GameObject preferred)
        {
            return ResolveShared(preferred);
        }

        public static GameObject ResolveDeathVfx(GameObject preferred, GameObject receivedHitFallback)
        {
            if (preferred != null)
            {
                return preferred;
            }

            if (receivedHitFallback != null)
            {
                return receivedHitFallback;
            }

            return LoadSharedHitVfx();
        }

        private static GameObject ResolveShared(GameObject preferred)
        {
            return preferred != null ? preferred : LoadSharedHitVfx();
        }

        private static GameObject LoadSharedHitVfx()
        {
            if (sharedHitVfxPrefab == null)
            {
                sharedHitVfxPrefab = Resources.Load<GameObject>(SharedHitVfxResourcePath);
            }

            return sharedHitVfxPrefab;
        }
    }
}
