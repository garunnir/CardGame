using System.Threading;
using TMPro;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    internal interface IHeroMotionHost
    {
        Transform Transform { get; }
        Transform ShakeRoot { get; }
        GameObject GameObject { get; }
        bool IsActiveAndEnabled { get; }
        bool IsMotionValid { get; }
        bool UseDotween { get; }

        float AttackDashDistance { get; }
        float AttackDashDuration { get; }
        float HpChangeDuration { get; }

        TextMeshPro HpLabel { get; }
        TextMeshPro MpLabel { get; }
        HeroHpShieldBarView HpShieldBar { get; }
        CardHpBarView MpBar { get; }

        int DisplayHp { get; set; }
        int DisplayShield { get; set; }
        int DisplayMp { get; set; }
        int DisplayMaxHp { get; }
        int DisplayMaxMp { get; }

        void SetStatsVisual(int hp, int shield, int mp);
        void RefreshStatsInstant();
        void CancelTransformMotion();
        CancellationToken GetDestroyCancellationToken();
    }
}
