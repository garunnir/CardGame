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
        ApplyPrimaryDamage,
        PlayHitPresentation,
        HitShake,
        HpBarTween,
        PlayCounterPresentation,
        ApplyCounterDamage,
        WaitBeforeSecondary,
        PlaySecondaryHitPresentation,
        ApplySecondaryDamage,
        CameraShake,
        PlayTurnHealPresentation,
    }

    public readonly struct PresentationCue
    {
        public PresentationCue(
            PresentationCueKind kind,
            float duration = 0f,
            float floatParam = 0f,
            CardGame.CardBattle.Cards.CardModel subject = null)
        {
            Kind = kind;
            Duration = duration;
            FloatParam = floatParam;
            Subject = subject;
        }

        public PresentationCueKind Kind { get; }
        public float Duration { get; }
        public float FloatParam { get; }
        public CardGame.CardBattle.Cards.CardModel Subject { get; }
    }
}
