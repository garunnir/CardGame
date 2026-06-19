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
        PlayOnHitPresentation,
        PlayReceivedHitPresentation,
        HitShake,
        HpBarTween,
        PlayCounterOnHitPresentation,
        WaitBeforeSecondary,
        PlaySecondaryOnHitPresentation,
        CameraShake,
        PlayHealOnTargetPresentation,
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
            int hpTo = -1,
            CardInstanceId sourceId = default)
        {
            Kind = kind;
            Duration = duration;
            FloatParam = floatParam;
            SubjectId = subjectId;
            HpFrom = hpFrom;
            HpTo = hpTo;
            SourceId = sourceId;
        }

        public PresentationCueKind Kind { get; }
        public float Duration { get; }
        public float FloatParam { get; }
        public CardInstanceId SubjectId { get; }
        public int HpFrom { get; }
        public int HpTo { get; }
        public CardInstanceId SourceId { get; }
    }
}
