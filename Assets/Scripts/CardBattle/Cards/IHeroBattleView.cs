using System;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>전투 연출에 필요한 영웅 3D 뷰 계약.</summary>
    public interface IHeroBattleView
    {
        HeroInstanceId InstanceId { get; }
        Transform ViewTransform { get; }

        void Bind(HeroViewState state);
        void RefreshStatsInstant();
        void SetStats(int hp, int shield, int mp);
        void SetHpDisplay(int hp);
        void PlayHpChange(int fromHp, int toHp, Action onComplete = null);
        void PlayAttackDash(
            Vector3 worldTarget,
            float dashDuration,
            Action onImpact,
            Action onComplete = null);
        void PlayHitShake(float strength, Action onComplete = null);
        UniTask TweenHpShieldAsync(
            int fromHp,
            int toHp,
            int fromShield,
            int toShield,
            int maxHp,
            float duration = -1f);
        UniTask TweenMpAsync(int fromMp, int toMp, int maxMp, float duration = -1f);
        void SetTargetHighlight(bool enabled);
        void ApplyLayout(BattleLayoutConfig layoutConfig);
    }

}
