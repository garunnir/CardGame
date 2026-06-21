using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    internal static class HeroPresentationClipResolver
    {
        public static void ResolveReceivedHit(HeroModel hero, out AudioClip sfx, out GameObject vfx)
        {
            sfx = null;
            vfx = null;
            if (hero?.NormalAttackBehavior == null)
            {
                return;
            }

            var presentation = hero.NormalAttackBehavior.presentation;
            sfx = presentation.receivedHitSfx;
            vfx = CardPresentationDefaults.ResolveReceivedHitVfx(presentation.receivedHitVfxPrefab);
        }

        public static void ResolveStrike(HeroModel hero, out AudioClip sfx, out GameObject vfx)
        {
            sfx = null;
            vfx = null;
            if (hero?.NormalAttackBehavior == null)
            {
                return;
            }

            var presentation = hero.NormalAttackBehavior.presentation;
            sfx = presentation.strikeSfx;
            vfx = presentation.strikeVfxPrefab;
        }

        public static void ResolveShieldBuff(HeroModel hero, out AudioClip sfx, out GameObject vfx)
        {
            sfx = null;
            vfx = null;
            if (hero?.ShieldBehavior == null)
            {
                return;
            }

            var presentation = hero.ShieldBehavior.presentation;
            sfx = presentation.shieldBuffSfx;
            vfx = presentation.shieldBuffVfxPrefab;
        }
    }
}
