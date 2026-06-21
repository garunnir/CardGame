using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    public sealed partial class HeroEntity
    {
        public void PlayHpChange(int fromHp, int toHp, Action onComplete = null)
        {
            combatMotion.PlayHpChange(fromHp, toHp, onComplete);
        }

        public void PlayAttackDash(
            Vector3 worldTarget,
            float dashDuration,
            Action onImpact,
            Action onComplete = null)
        {
            combatMotion.PlayAttackDash(worldTarget, dashDuration, onImpact, onComplete);
        }

        public void PlayHitShake(float strength, Action onComplete = null)
        {
            combatMotion.PlayHitShake(strength, onComplete);
        }

        public UniTask TweenHpShieldAsync(
            int fromHp,
            int toHp,
            int fromShield,
            int toShield,
            int maxHp,
            float duration = -1f)
        {
            var tweenDuration = duration > 0f ? duration : HeroCombatMotion.DefaultTweenDuration;
            displayMaxHp = maxHp > 0 ? maxHp : displayMaxHp;
            return combatMotion.TweenHpShieldAsync(fromHp, toHp, fromShield, toShield, maxHp, tweenDuration);
        }

        public UniTask TweenMpAsync(int fromMp, int toMp, int maxMp, float duration = -1f)
        {
            var tweenDuration = duration > 0f ? duration : HeroCombatMotion.DefaultTweenDuration;
            displayMaxMp = maxMp > 0 ? maxMp : displayMaxMp;
            return combatMotion.TweenMpAsync(fromMp, toMp, maxMp, tweenDuration);
        }
    }
}
