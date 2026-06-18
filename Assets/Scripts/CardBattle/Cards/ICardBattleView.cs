using System;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>전투 연출·바인딩에 필요한 카드 뷰 계약 (uGUI / 3D 공용).</summary>
    public interface ICardBattleView
    {
        CardModel BoundModel { get; }
        Transform ViewTransform { get; }

        void Bind(CardModel model);
        void RefreshHpInstant();
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
