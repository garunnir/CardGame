using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Presentation
{
    public enum PresentationCueKind
    {
        Wait,
        UiAttackBloom,
        UiHealerBloom,
        PlayAttackPresentation,
        PlayShootPresentation,
        AttackDash,
        PlayHitPresentation,
        HitShake,
        HpBarTween,
        PlayCounterPresentation,
        WaitBeforeSecondary,
        PlaySecondaryHitPresentation,
        CameraShake,
        PlayTurnHealPresentation,
        PlayDeathPresentation,
    }

    public readonly struct PresentationCue
    {
        public PresentationCue(
            PresentationCueKind kind,
            float duration = 0f,
            float floatParam = 0f,
            CardInstanceId subjectId = default,
            int hpFrom = -1,
            int hpTo = -1)
        {
            Kind = kind;
            Duration = duration;
            FloatParam = floatParam;
            SubjectId = subjectId;
            HpFrom = hpFrom;
            HpTo = hpTo;
        }

        public PresentationCueKind Kind { get; }
        public float Duration { get; }
        public float FloatParam { get; }
        public CardInstanceId SubjectId { get; }
        public int HpFrom { get; }
        public int HpTo { get; }
    }
}
