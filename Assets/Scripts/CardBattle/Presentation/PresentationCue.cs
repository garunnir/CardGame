using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Presentation
{
    public enum PresentationCueKind
    {
        Wait,
        UiAttackBloom,
        UiHealerBloom,
        PlayAttackPresentation,
        FlyProjectile,
        PlayProjectileImpact,
        PlayStatFloatingText,
        AttackDash,
        PlayOnHitPresentation,
        PlayReceivedHitPresentation,
        HitShake,
        HpBarTween,
        PlayCounterOnHitPresentation,
        WaitBeforeSecondary,
        PlaySecondaryOnHitPresentation,
        CameraShake,
        PlayDeathPresentation,
        PlayHeroStrikePresentation,
        PlayHeroShieldBuffPresentation,
        PlayHeroReceivedHitPresentation,
        HeroStatTween,
        PlayHeroSupportFromSlot,
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
            CardInstanceId sourceId = default,
            HeroInstanceId subjectHeroId = default,
            HeroInstanceId sourceHeroId = default,
            int shieldFrom = -1,
            int shieldTo = -1,
            int mpFrom = -1,
            int mpTo = -1,
            bool isMpGain = false,
            ProjectilePresentationAsset projectilePresentation = null,
            ProjectileRole projectileRole = default,
            StatFeedbackKind statFeedbackKind = default,
            int statAmount = 0)
        {
            Kind = kind;
            Duration = duration;
            FloatParam = floatParam;
            SubjectId = subjectId;
            HpFrom = hpFrom;
            HpTo = hpTo;
            SourceId = sourceId;
            SubjectHeroId = subjectHeroId;
            SourceHeroId = sourceHeroId;
            ShieldFrom = shieldFrom;
            ShieldTo = shieldTo;
            MpFrom = mpFrom;
            MpTo = mpTo;
            IsMpGain = isMpGain;
            ProjectilePresentation = projectilePresentation;
            ProjectileRole = projectileRole;
            StatFeedbackKind = statFeedbackKind;
            StatAmount = statAmount;
        }

        public PresentationCueKind Kind { get; }
        public float Duration { get; }
        public float FloatParam { get; }
        public CardInstanceId SubjectId { get; }
        public int HpFrom { get; }
        public int HpTo { get; }
        public CardInstanceId SourceId { get; }
        public HeroInstanceId SubjectHeroId { get; }
        public HeroInstanceId SourceHeroId { get; }
        public int ShieldFrom { get; }
        public int ShieldTo { get; }
        public int MpFrom { get; }
        public int MpTo { get; }
        public bool IsMpGain { get; }
        public ProjectilePresentationAsset ProjectilePresentation { get; }
        public ProjectileRole ProjectileRole { get; }
        public StatFeedbackKind StatFeedbackKind { get; }
        public int StatAmount { get; }
    }
}
