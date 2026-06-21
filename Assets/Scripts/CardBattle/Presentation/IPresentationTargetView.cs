using System;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>카드·영웅 패널 공통 연출 앵커.</summary>
    public interface IPresentationTargetView
    {
        Transform ViewTransform { get; }

        void SetHpDisplay(int hp);
        void PlayHpChange(int fromHp, int toHp, Action onComplete = null);
        void PlayHitShake(float strength, Action onComplete = null);
        void PlayAttackDash(
            Vector3 worldTarget,
            float dashDuration,
            Action onImpact,
            Action onComplete = null);
    }

    public sealed class CardPresentationTargetAdapter : IPresentationTargetView
    {
        private readonly Cards.ICardBattleView view;

        public CardPresentationTargetAdapter(Cards.ICardBattleView view)
        {
            this.view = view;
        }

        public Transform ViewTransform => view?.ViewTransform;

        public void SetHpDisplay(int hp) => view?.SetHpDisplay(hp);

        public void PlayHpChange(int fromHp, int toHp, Action onComplete = null)
            => view?.PlayHpChange(fromHp, toHp, onComplete);

        public void PlayHitShake(float strength, Action onComplete = null)
            => view?.PlayHitShake(strength, onComplete);

        public void PlayAttackDash(
            Vector3 worldTarget,
            float dashDuration,
            Action onImpact,
            Action onComplete = null)
            => view?.PlayAttackDash(worldTarget, dashDuration, onImpact, onComplete);
    }

    public sealed class HeroPresentationTargetAdapter : IPresentationTargetView
    {
        private readonly Cards.IHeroBattleView view;

        public HeroPresentationTargetAdapter(Cards.IHeroBattleView view)
        {
            this.view = view;
        }

        public Transform ViewTransform => view?.ViewTransform;

        public void SetHpDisplay(int hp) => view?.SetHpDisplay(hp);

        public void PlayHpChange(int fromHp, int toHp, Action onComplete = null)
            => view?.PlayHpChange(fromHp, toHp, onComplete);

        public void PlayHitShake(float strength, Action onComplete = null)
            => view?.PlayHitShake(strength, onComplete);

        public void PlayAttackDash(
            Vector3 worldTarget,
            float dashDuration,
            Action onImpact,
            Action onComplete = null)
            => view?.PlayAttackDash(worldTarget, dashDuration, onImpact, onComplete);
    }
}
