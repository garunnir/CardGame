using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    public sealed partial class HeroEntity : IHeroMotionHost
    {
        Transform IHeroMotionHost.Transform => transform;
        Transform IHeroMotionHost.ShakeRoot => shakeRoot;
        GameObject IHeroMotionHost.GameObject => gameObject;
        bool IHeroMotionHost.IsActiveAndEnabled => isActiveAndEnabled;
        bool IHeroMotionHost.IsMotionValid => IsMotionValid;
        bool IHeroMotionHost.UseDotween => useDotween;

        float IHeroMotionHost.AttackDashDistance => attackDashDistance;
        float IHeroMotionHost.AttackDashDuration => attackDashDuration;
        float IHeroMotionHost.HpChangeDuration => hpChangeDuration;

        TextMeshPro IHeroMotionHost.HpLabel => hpLabel;
        TextMeshPro IHeroMotionHost.MpLabel => mpLabel;
        HeroHpShieldBarView IHeroMotionHost.HpShieldBar => hpShieldBar;
        CardHpBarView IHeroMotionHost.MpBar => mpBar;

        int IHeroMotionHost.DisplayHp
        {
            get => displayHp;
            set => displayHp = value;
        }

        int IHeroMotionHost.DisplayShield
        {
            get => displayShield;
            set => displayShield = value;
        }

        int IHeroMotionHost.DisplayMp
        {
            get => displayMp;
            set => displayMp = value;
        }

        int IHeroMotionHost.DisplayMaxHp => displayMaxHp;
        int IHeroMotionHost.DisplayMaxMp => displayMaxMp;

        void IHeroMotionHost.SetStatsVisual(int hp, int shield, int mp) => SetStatsVisual(hp, shield, mp);
        void IHeroMotionHost.RefreshStatsInstant() => RefreshStatsInstant();

        void IHeroMotionHost.CancelTransformMotion()
        {
            transform.DOKill();
            if (shakeRoot != null && shakeRoot != transform)
            {
                shakeRoot.DOKill();
                shakeRoot.localPosition = Vector3.zero;
            }
        }

        CancellationToken IHeroMotionHost.GetDestroyCancellationToken()
        {
            return this.GetCancellationTokenOnDestroy();
        }
    }
}
