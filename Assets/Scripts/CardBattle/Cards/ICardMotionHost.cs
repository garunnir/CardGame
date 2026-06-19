using System.Threading;
using TMPro;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>motion 드라이버가 CardEntity 상태·비주얼 API에 접근하기 위한 내부 호스트.</summary>
    internal interface ICardMotionHost
    {
        Transform Transform { get; }
        Transform ShakeRoot { get; }
        GameObject GameObject { get; }
        bool IsActiveAndEnabled { get; }
        CardBoardPhase Phase { get; }
        bool IsMotionValid { get; }
        bool UseDotween { get; }

        float DeployMoveDuration { get; }
        float FlipDuration { get; }
        float AttackDashDistance { get; }
        float AttackDashDuration { get; }
        float HpChangeDuration { get; }
        float DeathVisualDuration { get; }

        Vector3 HomeLocalPosition { get; set; }
        Quaternion HomeLocalRotation { get; set; }

        TextMeshPro HpLabel { get; }
        int DisplayHp { get; set; }

        void SetPhase(CardBoardPhase phase);
        void SetFaceDownInstant();
        void SetFaceUpInstant();
        void SetFrontLabelsVisible(bool visible);
        void RefreshHpInstant();
        void CancelTransformMotion();
        CancellationToken GetDestroyCancellationToken();
    }
}
