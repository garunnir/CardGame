using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using CardGame.CardBattle.UI;

namespace CardGame.CardBattle.Presentation
{
    public enum PresentationKind
    {
        CardVsCard,
        CardVsHero,
        HeroStrike,
    }

    /// <summary>공격·HeroStrike 1회 연출용 통합 스냅샷.</summary>
    public sealed class PresentationSnapshot
    {
        private readonly Dictionary<int, (int before, int after)> cardHpById = new Dictionary<int, (int, int)>();
        private readonly Dictionary<int, HeroStatSnapshot> heroStatsById = new Dictionary<int, HeroStatSnapshot>();

        public readonly struct HeroStatSnapshot
        {
            public HeroStatSnapshot(int beforeHp, int afterHp, int beforeShield, int afterShield, int beforeMp, int afterMp)
            {
                BeforeHp = beforeHp;
                AfterHp = afterHp;
                BeforeShield = beforeShield;
                AfterShield = afterShield;
                BeforeMp = beforeMp;
                AfterMp = afterMp;
            }

            public int BeforeHp { get; }
            public int AfterHp { get; }
            public int BeforeShield { get; }
            public int AfterShield { get; }
            public int BeforeMp { get; }
            public int AfterMp { get; }
        }

        public static PresentationSnapshot FromCardAttack(AttackOutcome outcome, BattleActionRequest request)
        {
            var snapshot = new PresentationSnapshot();
            snapshot.RecordCard(
                request.Attacker.InstanceId,
                outcome.BeforeAttackerHp,
                request.Attacker.CurrentHp);
            snapshot.RecordCard(
                request.Target.InstanceId,
                outcome.BeforeTargetHp,
                request.Target.CurrentHp);

            var secondary = outcome.Resolution.Secondary;
            if (secondary.HasTarget)
            {
                snapshot.RecordCard(
                    secondary.Target.InstanceId,
                    outcome.BeforeSecondaryHp,
                    secondary.Target.CurrentHp);
            }

            return snapshot;
        }

        public static PresentationSnapshot FromCardHeroAttack(
            CardHeroAttackOutcome outcome,
            CardModel attacker,
            HeroModel hero)
        {
            var snapshot = new PresentationSnapshot();
            snapshot.RecordCard(attacker.InstanceId, outcome.BeforeAttackerHp, attacker.CurrentHp);
            snapshot.RecordHero(
                hero.InstanceId,
                outcome.BeforeHeroHp,
                hero.CurrentHp,
                outcome.BeforeHeroShield,
                hero.CurrentShield,
                hero.CurrentMp,
                hero.CurrentMp);
            return snapshot;
        }

        public static PresentationSnapshot FromHeroStrike(
            HeroStrikeResult result,
            int beforeStrikerHp,
            int beforeStrikerShield,
            int beforeStrikerMp,
            int beforeDefenderHp,
            int beforeDefenderShield,
            int beforeDefenderMp)
        {
            var snapshot = new PresentationSnapshot();
            if (result.Striker != null)
            {
                snapshot.RecordHero(
                    result.Striker.InstanceId,
                    beforeStrikerHp,
                    result.Striker.CurrentHp,
                    beforeStrikerShield,
                    result.Striker.CurrentShield,
                    beforeStrikerMp,
                    result.Striker.CurrentMp);
            }

            if (result.Defender != null)
            {
                snapshot.RecordHero(
                    result.Defender.InstanceId,
                    beforeDefenderHp,
                    result.Defender.CurrentHp,
                    beforeDefenderShield,
                    result.Defender.CurrentShield,
                    beforeDefenderMp,
                    result.Defender.CurrentMp);
            }

            return snapshot;
        }

        public void RecordCard(CardInstanceId id, int beforeHp, int afterHp)
        {
            if (!id.IsValid)
            {
                return;
            }

            cardHpById[id.Value] = (beforeHp, afterHp);
        }

        public void RecordHero(
            HeroInstanceId id,
            int beforeHp,
            int afterHp,
            int beforeShield,
            int afterShield,
            int beforeMp,
            int afterMp)
        {
            if (!id.IsValid)
            {
                return;
            }

            heroStatsById[id.Value] = new HeroStatSnapshot(
                beforeHp,
                afterHp,
                beforeShield,
                afterShield,
                beforeMp,
                afterMp);
        }

        public bool TryGetCardHp(CardInstanceId id, out int beforeHp, out int afterHp)
        {
            if (id.IsValid && cardHpById.TryGetValue(id.Value, out var pair))
            {
                beforeHp = pair.before;
                afterHp = pair.after;
                return true;
            }

            beforeHp = 0;
            afterHp = 0;
            return false;
        }

        public int GetBeforeCardHp(CardInstanceId id)
            => TryGetCardHp(id, out var before, out _) ? before : 0;

        public int GetAfterCardHp(CardInstanceId id)
            => TryGetCardHp(id, out _, out var after) ? after : 0;

        public bool TryGetHeroStats(HeroInstanceId id, out HeroStatSnapshot stats)
        {
            if (id.IsValid && heroStatsById.TryGetValue(id.Value, out stats))
            {
                return true;
            }

            stats = default;
            return false;
        }
    }

    public sealed class BattlePresentationSpec
    {
        public BattlePresentationSpec(
            PresentationKind kind,
            PresentationSnapshot snapshot,
            ICardViewRegistry cardViewRegistry,
            HeroArenaPresenter heroPresenter,
            UIManager ui,
            CardPresentationService presentation,
            float tailDelay = 0.55f)
        {
            Kind = kind;
            Snapshot = snapshot;
            CardViewRegistry = cardViewRegistry;
            HeroPresenter = heroPresenter;
            Ui = ui;
            Presentation = presentation;
            TailDelay = tailDelay;
        }

        public PresentationKind Kind { get; }
        public PresentationSnapshot Snapshot { get; }
        public ICardViewRegistry CardViewRegistry { get; }
        public HeroArenaPresenter HeroPresenter { get; }
        public UIManager Ui { get; }
        public CardPresentationService Presentation { get; }
        public float TailDelay { get; }

        public CardModel AttackerCard { get; set; }
        public CardModel PrimaryTargetCard { get; set; }
        public HeroModel PrimaryTargetHero { get; set; }
        public CardBehaviorAsset CardBehavior { get; set; }
        public HeroBehaviorAsset HeroBehavior { get; set; }
        public AttackOutcome CardOutcome { get; set; }
        public CardHeroAttackOutcome HeroAttackOutcome { get; set; }
        public HeroStrikeResult HeroStrikeResult { get; set; }
        public AttackResolution CardResolution => CardOutcome.Resolution;
        public BattleActionResult ActionResult { get; set; }

        public HeroModel StrikerHero { get; set; }
        public HeroModel DefenderHero { get; set; }

        public ICardBattleView GetCardView(CardInstanceId id)
        {
            return CardViewRegistry != null && CardViewRegistry.TryGetView(id, out var view) ? view : null;
        }

        public CardModel GetCardModel(CardInstanceId id)
        {
            return CardViewRegistry != null && CardViewRegistry.TryGetModel(id, out var model) ? model : null;
        }

        public IPresentationTargetView GetCardTargetView(CardInstanceId id)
        {
            var view = GetCardView(id);
            return view != null ? new CardPresentationTargetAdapter(view) : null;
        }

        public IPresentationTargetView GetHeroTargetView(HeroInstanceId id)
        {
            return HeroPresenter != null ? HeroPresenter.GetPresentationView(id) : null;
        }

        public IPresentationTargetView GetHeroTargetView(HeroModel hero)
        {
            return hero != null ? GetHeroTargetView(hero.InstanceId) : null;
        }
    }
}
