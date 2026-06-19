using System;
using CardGame.CardBattle.Core;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>전투 연출에 필요한 카드 뷰 계약 — CardModel 참조 없음.</summary>
    public interface ICardBattleView
    {
        CardInstanceId InstanceId { get; }
        Transform ViewTransform { get; }

        void Bind(CardViewState state);
        void RefreshHpInstant();
        void SetHpDisplay(int hp);
        void PlayHpChange(int fromHp, int toHp, Action onComplete = null);
        void PlayAttackDash(
            Vector3 worldTarget,
            float dashDuration,
            Action onImpact,
            Action onComplete = null);
        void PlayHitShake(float strength, Action onComplete = null);
        void PlayDeathVisual(Action onComplete = null);
    }
}
