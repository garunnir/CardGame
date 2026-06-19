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
        private readonly PresentationPlayer presentationPlayer;
        private readonly CardPresentationService presentationService;
        private readonly float battlePresentationDelay;

        public BattleOrchestrator(
            BattleField field,
            PresentationPlayer presentationPlayer,
            CardPresentationService presentationService,
            float battlePresentationDelay)
        {
            this.field = field;
            this.presentationPlayer = presentationPlayer;
            this.presentationService = presentationService;
            this.battlePresentationDelay = battlePresentationDelay;
        }

        public async UniTask<BattleOrchestrationResult> ExecuteAsync(
            BattleActionRequest request,
            ICardBoardSession boardSession,
            UIManager uiManager)
        {
            if (request.Attacker == null
                || request.Target == null
                || !request.Attacker.IsAlive
                || !request.Target.IsAlive)
            {
                return BattleOrchestrationResult.Continue(default);
            }

            var enemyField = request.Attacker.IsPlayerTeam
                ? field.EnemyBattlefield
                : field.PlayerBattlefield;

            var outcome = BattleResolver.PlanOutcome(request, enemyField);
            var actionResult = BattleCommandExecutor.ApplyAttack(outcome.Resolution, request);
            var snapshot = AttackPresentationSnapshot.From(outcome, request);
            ICardViewRegistry viewRegistry = boardSession as ICardViewRegistry;

            var presentationContext = new PresentationContext(
                request,
                outcome,
                snapshot,
                actionResult,
                viewRegistry,
                uiManager,
                presentationService);

            var sequence = PresentationSequenceBuilder.BuildAttack(
                presentationContext,
                battlePresentationDelay);

            if (boardSession != null)
            {
                await boardSession.RunExclusiveAsync(async () =>
                {
                    if (presentationPlayer != null)
                    {
                        await presentationPlayer.PlayAttackAsync(presentationContext, sequence);
                    }

                    field.ProcessDeathsAndRefill(true);
                    field.ProcessDeathsAndRefill(false);
                    await boardSession.SyncBoardWithinLockAsync(field, animateRefill: true);
                });
            }
            else if (presentationPlayer != null)
            {
                await presentationPlayer.PlayAttackAsync(presentationContext, sequence);
                field.ProcessDeathsAndRefill(true);
                field.ProcessDeathsAndRefill(false);
            }
            else
            {
                field.ProcessDeathsAndRefill(true);
                field.ProcessDeathsAndRefill(false);
            }

            if (field.IsTeamDefeated(true) || field.IsTeamDefeated(false))
            {
                return BattleOrchestrationResult.GameOver(actionResult, field.IsTeamDefeated(false));
            }

            return BattleOrchestrationResult.Continue(actionResult);
        }
    }
}
