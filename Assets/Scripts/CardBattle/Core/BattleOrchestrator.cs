using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Presentation;
using CardGame.CardBattle.UI;
using Cysharp.Threading.Tasks;

namespace CardGame.CardBattle.Core
{
    public readonly struct BattleOrchestrationResult
    {
        public BattleOrchestrationResult(
            bool continueBattle,
            bool playerWon,
            BattleActionResult actionResult)
        {
            ContinueBattle = continueBattle;
            PlayerWon = playerWon;
            ActionResult = actionResult;
        }

        public bool ContinueBattle { get; }
        public bool PlayerWon { get; }
        public BattleActionResult ActionResult { get; }

        public static BattleOrchestrationResult Continue(BattleActionResult actionResult)
        {
            return new BattleOrchestrationResult(true, false, actionResult);
        }

        public static BattleOrchestrationResult GameOver(BattleActionResult actionResult, bool playerWon)
        {
            return new BattleOrchestrationResult(false, playerWon, actionResult);
        }
    }

    /// <summary>공격 1회 파이프라인 — 연산·도메인 적용·연출·리필·승패 판정.</summary>
    public sealed class BattleOrchestrator
    {
        private readonly BattleField field;
        private readonly HeroArenaField heroArena;
        private readonly HeroStrikeController heroStrikeController;
        private readonly PresentationPlayer presentationPlayer;
        private readonly CardPresentationService presentationService;
        private readonly float battlePresentationDelay;

        public BattleOrchestrator(
            BattleField field,
            HeroArenaField heroArena,
            HeroStrikeController heroStrikeController,
            PresentationPlayer presentationPlayer,
            CardPresentationService presentationService,
            float battlePresentationDelay)
        {
            this.field = field;
            this.heroArena = heroArena;
            this.heroStrikeController = heroStrikeController;
            this.presentationPlayer = presentationPlayer;
            this.presentationService = presentationService;
            this.battlePresentationDelay = battlePresentationDelay;
        }

        public async UniTask<BattleOrchestrationResult> ExecuteAsync(
            BattleActionRequest request,
            ICardBoardSession boardSession,
            UIManager uiManager,
            HeroArenaPresenter heroPresenter,
            System.Action<HeroStrikeResult> onHeroStrike = null,
            System.Action syncHeroViews = null)
        {
            if (request.Attacker == null || !request.Attacker.IsAlive)
            {
                return BattleOrchestrationResult.Continue(default);
            }

            BattleActionResult actionResult;
            CardHeroAttackOutcome heroPlanOutcome = default;
            AttackOutcome cardPlanOutcome = default;

            if (request.TargetsHero)
            {
                if (request.HeroTarget == null || !request.HeroTarget.IsAlive)
                {
                    return BattleOrchestrationResult.Continue(default);
                }

                heroPlanOutcome = BattleResolver.PlanHeroOutcome(request);
                actionResult = BattleResolver.ApplyHeroOutcome(heroPlanOutcome, request);
            }
            else
            {
                if (request.Target == null || !request.Target.IsAlive)
                {
                    return BattleOrchestrationResult.Continue(default);
                }

                var enemyField = request.Attacker.IsPlayerTeam
                    ? field.EnemyBattlefield
                    : field.PlayerBattlefield;
                cardPlanOutcome = BattleResolver.PlanCardOutcome(request, enemyField);
                actionResult = BattleResolver.ApplyCardOutcome(cardPlanOutcome, request);
            }

            ICardViewRegistry viewRegistry = boardSession as ICardViewRegistry;
            var spec = BuildPresentationSpec(
                request,
                heroPlanOutcome,
                cardPlanOutcome,
                actionResult,
                viewRegistry,
                heroPresenter,
                uiManager);

            if (boardSession != null)
            {
                await boardSession.RunExclusiveAsync(async () =>
                {
                    await ExecutePresentationAsync(spec);
                    field.ProcessDeathsAndRefill(true);
                    field.ProcessDeathsAndRefill(false);
                    await boardSession.SyncBoardWithinLockAsync(field, animateRefill: true);
                });
            }
            else
            {
                await ExecutePresentationAsync(spec);
                field.ProcessDeathsAndRefill(true);
                field.ProcessDeathsAndRefill(false);
            }

            syncHeroViews?.Invoke();
            return await FinalizeAfterAttackAsync(
                request.Attacker.IsPlayerTeam,
                actionResult,
                uiManager,
                heroPresenter,
                viewRegistry,
                onHeroStrike,
                syncHeroViews);
        }

        private BattlePresentationSpec BuildPresentationSpec(
            BattleActionRequest request,
            CardHeroAttackOutcome heroPlanOutcome,
            AttackOutcome cardPlanOutcome,
            BattleActionResult actionResult,
            ICardViewRegistry viewRegistry,
            HeroArenaPresenter heroPresenter,
            UIManager uiManager)
        {
            if (request.TargetsHero)
            {
                var snapshot = PresentationSnapshot.FromCardHeroAttack(
                    heroPlanOutcome,
                    request.Attacker,
                    request.HeroTarget);

                return new BattlePresentationSpec(
                    PresentationKind.CardVsHero,
                    snapshot,
                    viewRegistry,
                    heroPresenter,
                    uiManager,
                    presentationService,
                    battlePresentationDelay)
                {
                    AttackerCard = request.Attacker,
                    PrimaryTargetHero = request.HeroTarget,
                    CardBehavior = request.Attacker.Behavior,
                    HeroAttackOutcome = heroPlanOutcome,
                    ActionResult = actionResult,
                };
            }

            var cardSnapshot = PresentationSnapshot.FromCardAttack(cardPlanOutcome, request);
            return new BattlePresentationSpec(
                PresentationKind.CardVsCard,
                cardSnapshot,
                viewRegistry,
                heroPresenter,
                uiManager,
                presentationService,
                battlePresentationDelay)
            {
                AttackerCard = request.Attacker,
                PrimaryTargetCard = request.Target,
                CardBehavior = request.Attacker.Behavior,
                CardOutcome = cardPlanOutcome,
                ActionResult = actionResult,
            };
        }

        private async UniTask ExecutePresentationAsync(BattlePresentationSpec spec)
        {
            if (presentationPlayer == null || spec == null)
            {
                return;
            }

            var sequence = PresentationSequenceBuilder.Build(spec);
            await presentationPlayer.PlayAsync(spec, sequence);
        }

        private async UniTask<BattleOrchestrationResult> FinalizeAfterAttackAsync(
            bool attackerIsPlayerTeam,
            BattleActionResult actionResult,
            UIManager uiManager,
            HeroArenaPresenter heroPresenter,
            ICardViewRegistry viewRegistry,
            System.Action<HeroStrikeResult> onHeroStrike,
            System.Action syncHeroViews)
        {
            var striker = heroArena?.GetHero(attackerIsPlayerTeam);
            var defender = heroArena?.GetOpponentHero(attackerIsPlayerTeam);
            var beforeStrikerHp = striker?.CurrentHp ?? 0;
            var beforeStrikerShield = striker?.CurrentShield ?? 0;
            var beforeStrikerMp = striker?.CurrentMp ?? 0;
            var beforeDefenderHp = defender?.CurrentHp ?? 0;
            var beforeDefenderShield = defender?.CurrentShield ?? 0;
            var beforeDefenderMp = defender?.CurrentMp ?? 0;

            var strikeResult = heroStrikeController.ExecuteAfterCardAttack(field, heroArena, attackerIsPlayerTeam);
            onHeroStrike?.Invoke(strikeResult);
            syncHeroViews?.Invoke();

            if (strikeResult.Striker != null && (strikeResult.DamageDealt > 0 || strikeResult.UsedShield))
            {
                var strikeSpec = BuildHeroStrikeSpec(
                    strikeResult,
                    beforeStrikerHp,
                    beforeStrikerShield,
                    beforeStrikerMp,
                    beforeDefenderHp,
                    beforeDefenderShield,
                    beforeDefenderMp,
                    viewRegistry,
                    heroPresenter,
                    uiManager);

                await ExecutePresentationAsync(strikeSpec);
                syncHeroViews?.Invoke();
            }

            if (heroArena != null && heroArena.IsEitherHeroDefeated)
            {
                var playerWon = !heroArena.IsHeroDefeated(true);
                return BattleOrchestrationResult.GameOver(actionResult, playerWon);
            }

            return BattleOrchestrationResult.Continue(actionResult);
        }

        private BattlePresentationSpec BuildHeroStrikeSpec(
            HeroStrikeResult strikeResult,
            int beforeStrikerHp,
            int beforeStrikerShield,
            int beforeStrikerMp,
            int beforeDefenderHp,
            int beforeDefenderShield,
            int beforeDefenderMp,
            ICardViewRegistry viewRegistry,
            HeroArenaPresenter heroPresenter,
            UIManager uiManager)
        {
            var snapshot = PresentationSnapshot.FromHeroStrike(
                strikeResult,
                beforeStrikerHp,
                beforeStrikerShield,
                beforeStrikerMp,
                beforeDefenderHp,
                beforeDefenderShield,
                beforeDefenderMp);

            HeroBehaviorAsset behavior = strikeResult.UsedShield
                ? strikeResult.Striker?.ShieldBehavior
                : strikeResult.Striker?.NormalAttackBehavior;

            return new BattlePresentationSpec(
                PresentationKind.HeroStrike,
                snapshot,
                viewRegistry,
                heroPresenter,
                uiManager,
                presentationService,
                tailDelay: strikeResult.UsedShield ? 0.15f : 0.2f)
            {
                StrikerHero = strikeResult.Striker,
                DefenderHero = strikeResult.Defender,
                HeroBehavior = behavior,
                HeroStrikeResult = strikeResult,
            };
        }
    }
}
