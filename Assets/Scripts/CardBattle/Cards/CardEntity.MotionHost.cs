using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    public sealed partial class CardEntity : ICardMotionHost
    {
        Transform ICardMotionHost.Transform => transform;
        Transform ICardMotionHost.ShakeRoot => shakeRoot;
        GameObject ICardMotionHost.GameObject => gameObject;
        bool ICardMotionHost.IsActiveAndEnabled => isActiveAndEnabled;
        CardBoardPhase ICardMotionHost.Phase => phase;
        bool ICardMotionHost.IsMotionValid => IsMotionValid;
        bool ICardMotionHost.UseDotween => useDotween;

        float ICardMotionHost.DeployMoveDuration => deployMoveDuration;
        float ICardMotionHost.FlipDuration => flipDuration;
        float ICardMotionHost.AttackDashDistance => attackDashDistance;
        float ICardMotionHost.AttackDashDuration => attackDashDuration;
        float ICardMotionHost.HpChangeDuration => hpChangeDuration;
        float ICardMotionHost.DeathVisualDuration => deathVisualDuration;

        Vector3 ICardMotionHost.HomeLocalPosition
        {
            get => homeLocalPosition;
            set => homeLocalPosition = value;
        }

        Quaternion ICardMotionHost.HomeLocalRotation
        {
            get => homeLocalRotation;
            set => homeLocalRotation = value;
        }

        TextMeshPro ICardMotionHost.HpLabel => hpLabel;

        int ICardMotionHost.DisplayHp
        {
            get => displayHp;
            set => displayHp = value;
        }

        void ICardMotionHost.SetPhase(CardBoardPhase nextPhase) => SetPhase(nextPhase);
        void ICardMotionHost.SetFaceDownInstant() => SetFaceDownInstant();
        void ICardMotionHost.SetFaceUpInstant() => SetFaceUpInstant();
        void ICardMotionHost.SetFrontLabelsVisible(bool visible) => SetFrontLabelsVisible(visible);
        void ICardMotionHost.RefreshHpInstant() => RefreshHpInstant();

        void ICardMotionHost.CancelTransformMotion()
        {
            transform.DOKill();
            if (shakeRoot != null && shakeRoot != transform)
            {
                shakeRoot.DOKill();
                shakeRoot.localPosition = Vector3.zero;
            }

            transform.localRotation = homeLocalRotation;
        }

        CancellationToken ICardMotionHost.GetDestroyCancellationToken()
        {
            return this.GetCancellationTokenOnDestroy();
        }
    }
}
