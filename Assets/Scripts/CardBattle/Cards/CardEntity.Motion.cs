using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    public sealed partial class CardEntity
    {
        public UniTask ApplyPlacement(
            Transform anchorParent,
            Vector3 targetLocalPosition,
            Quaternion targetLocalRotation,
            CardBoardPhase targetPhase,
            bool animate)
        {
            return boardMotion.ApplyPlacement(
                anchorParent,
                targetLocalPosition,
                targetLocalRotation,
                targetPhase,
                animate);
        }

        public void CancelMotion()
        {
            boardMotion.CancelMotion();
        }

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

        public void PlayDeathVisual(Action onComplete = null)
        {
            combatMotion.PlayDeathVisual(onComplete);
        }
    }
}
