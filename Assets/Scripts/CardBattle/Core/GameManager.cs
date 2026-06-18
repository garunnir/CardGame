using System;
using System.Collections.Generic;
using CardGame.CardBattle.Bridge;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Input;
using CardGame.CardBattle.Presentation;
using CardGame.CardBattle.States;
using CardGame.CardBattle.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.CardBattle.Core
{
    /// <summary>전투 FSM 및 데이터 허브. static 싱글톤 대신 주입/SerializeField 지원.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private UIManager uiManager;
        [SerializeField] private DragTargetingPresenter dragTargetingPresenter;
        [SerializeField] private CardBoardPresenter cardBoardPresenter;
        [SerializeField] private float battlePresentationDelay = 0.55f;

        private BaseState currentState;
        private UnityInputProvider inputProvider = new UnityInputProvider();
        private CardPresentationService presentationService;
        private PresentationPlayer presentationPlayer;

        public BattleField Field { get; } = new BattleField();
        public IInputProvider InputProvider => inputProvider;
        public List<CardDataAsset> PlayerDeckData { get; private set; } = new List<CardDataAsset>();
        public List<CardDataAsset> EnemyDeckData { get; private set; } = new List<CardDataAsset>();

        public BattleActionRequest PendingAction { get; set; }
        public bool IsPlayerTurn { get; set; }
        public BattleFlowStateId CurrentStateId => currentState?.StateId ?? BattleFlowStateId.Init;

        public Action<bool> OnBattleResult;
        public Action<bool> OnTurnChanged;
        public Action<CardModel> OnAttackerSelected;
        public Action<BattleActionResult> OnBattleResolvedEvent;
        public Action<bool> OnGameOverEvent;
        public Action OnReserveCountChanged;
        public Action OnHealerEffect;
        public DragTargetingPresenter DragTargetingPresenter => dragTargetingPresenter;

        private void Awake()
        {
            if (cardBoardPresenter != null)
            {
                cardBoardPresenter.InputHostsChanged += RefreshBoardInput;
            }
        }

        private void OnDestroy()
        {
            if (cardBoardPresenter != null)
            {
                cardBoardPresenter.InputHostsChanged -= RefreshBoardInput;
            }
        }

        public void ConfigureBoard(CardBoardPresenter presenter)
        {
            if (cardBoardPresenter != null)
            {
                cardBoardPresenter.InputHostsChanged -= RefreshBoardInput;
            }

            cardBoardPresenter = presenter;
            if (cardBoardPresenter != null)
            {
                cardBoardPresenter.InputHostsChanged += RefreshBoardInput;
            }
        }

        public void ConfigurePresentation(BattleAudioAdapter audioAdapter)
        {
            presentationService = new CardPresentationService(audioAdapter);
            presentationPlayer = new PresentationPlayer();
        }

        /// <summary>로비/외부 덱 데이터로 전투 시작.</summary>
        public void InitializeBattle(List<CardDataAsset> playerDeck, List<CardDataAsset> enemyDeck)
        {
            PlayerDeckData = SanitizeDeck(playerDeck, "Player");
            EnemyDeckData = SanitizeDeck(enemyDeck, "Enemy");
            ChangeState(new InitState(this));
        }

        public void RestartBattle()
        {
            InitializeBattle(PlayerDeckData, EnemyDeckData);
        }

        /// <summary>공격 연산 + 연출 + 사망/승패. true = 전투 계속.</summary>
        public async UniTask<bool> ExecuteBattleAsync(BattleActionRequest request)
        {
            if (request.Attacker == null
                || request.Target == null
                || !request.Attacker.IsAlive
                || !request.Target.IsAlive)
            {
                return true;
            }

            var enemyField = request.Attacker.IsPlayerTeam
                ? Field.EnemyBattlefield
                : Field.PlayerBattlefield;

            var beforeAttackerHp = request.Attacker.CurrentHp;
            var beforeTargetHp = request.Target.CurrentHp;
            var resolution = BattleResolver.Plan(
                request.Attacker,
                request.Target,
                enemyField);

            var presentationContext = new PresentationContext(
                request,
                resolution,
                beforeAttackerHp,
                beforeTargetHp,
                FindView,
                uiManager,
                presentationService);

            var sequence = PresentationSequenceBuilder.BuildAttack(
                presentationContext,
                battlePresentationDelay);

            BattleActionResult result;
            if (cardBoardPresenter != null)
            {
                await cardBoardPresenter.RunExclusiveAsync(async () =>
                {
                    result = presentationPlayer != null
                        ? await presentationPlayer.PlayAttackAsync(presentationContext, sequence)
                        : BattleResolver.Apply(resolution, request.Attacker, request.Target);

                    RaiseBattleResolved(result);

                    Field.ProcessDeathsAndRefill(true);
                    Field.ProcessDeathsAndRefill(false);
                    await cardBoardPresenter.SyncBoardWithinLockAsync(Field, animateRefill: true);
                });

                RefreshBoardInput();
                UpdateReserveUi();
            }
            else
            {
                result = presentationPlayer != null
                    ? await presentationPlayer.PlayAttackAsync(presentationContext, sequence)
                    : BattleResolver.Apply(resolution, request.Attacker, request.Target);

                RaiseBattleResolved(result);

                Field.ProcessDeathsAndRefill(true);
                Field.ProcessDeathsAndRefill(false);
            }

            RaiseReserveChanged();

            if (Field.IsTeamDefeated(true) || Field.IsTeamDefeated(false))
            {
                var playerWin = Field.IsTeamDefeated(false);
                ChangeState(new GameOverState(this, playerWin));
                return false;
            }

            return true;
        }

        public void ChangeState(BaseState nextState)
        {
            currentState?.Exit();
            currentState = nextState;
            currentState.Enter();
        }

        public UniTask BuildBoardViewsAsync()
        {
            if (cardBoardPresenter == null)
            {
                return UniTask.CompletedTask;
            }

            return RunBoardPresentationAsync(
                cardBoardPresenter.BuildBoardAsync(Field),
                refreshInput: true);
        }

        public UniTask SyncBoardViewsAsync(bool animateRefill = false)
        {
            if (cardBoardPresenter == null)
            {
                return UniTask.CompletedTask;
            }

            return RunBoardPresentationAsync(
                cardBoardPresenter.SyncBoardAsync(Field, animateRefill),
                refreshInput: true);
        }

        public UniTask SyncAllViewsAsync()
        {
            return SyncBoardViewsAsync(animateRefill: false);
        }

        private async UniTask RunBoardPresentationAsync(UniTask boardTask, bool refreshInput)
        {
            await boardTask;
            if (refreshInput)
            {
                RefreshBoardInput();
            }

            UpdateReserveUi();
        }

        public void BuildBoardViews()
        {
            BuildBoardViewsAsync().Forget();
        }

        public void SyncBoardViews(bool animateRefill = false)
        {
            SyncBoardViewsAsync(animateRefill).Forget();
        }

        public void SyncAllViews()
        {
            SyncAllViewsAsync().Forget();
        }

        private void RefreshBoardInput()
        {
            if (cardBoardPresenter != null)
            {
                inputProvider.BindInputHosts(cardBoardPresenter.InputHosts);
            }
        }

        private void UpdateReserveUi()
        {
            uiManager?.UpdateReserveCounts(
                Field.PlayerReserve.Count,
                Field.EnemyReserve.Count);
        }

        public void RaiseTurnBanner(bool isPlayerTurn)
        {
            OnTurnChanged?.Invoke(isPlayerTurn);
            uiManager?.ShowTurnBanner(isPlayerTurn);
        }

        public void RaiseAttackerSelected(CardModel card)
        {
            OnAttackerSelected?.Invoke(card);
        }

        public void RaiseBattleResolved(BattleActionResult result)
        {
            OnBattleResolvedEvent?.Invoke(result);
        }

        public void RaiseGameOver(bool playerWin)
        {
            OnGameOverEvent?.Invoke(playerWin);
            uiManager?.ShowResultPopup(playerWin);
        }

        public void RaiseReserveChanged()
        {
            OnReserveCountChanged?.Invoke();
            uiManager?.UpdateReserveCounts(
                Field.PlayerReserve.Count,
                Field.EnemyReserve.Count);
        }

        public void RaiseHealerPulse()
        {
            OnHealerEffect?.Invoke();
            var field = IsPlayerTurn ? Field.PlayerBattlefield : Field.EnemyBattlefield;
            PlayTurnStartPresentationAsync(field).Forget();
        }

        private async UniTaskVoid PlayTurnStartPresentationAsync(CardModel[] battlefield)
        {
            if (presentationPlayer == null)
            {
                presentationService?.PlayHealForTeam(battlefield, FindView);
                return;
            }

            var sequence = PresentationSequenceBuilder.BuildTurnStartHeal(battlefield);
            await presentationPlayer.PlayTurnStartAsync(
                sequence,
                uiManager,
                presentationService,
                FindView);
        }

        public ICardBattleView FindView(CardModel model)
        {
            return cardBoardPresenter != null
                ? cardBoardPresenter.FindView(model)
                : null;
        }

        private static List<CardDataAsset> SanitizeDeck(List<CardDataAsset> deck, string teamPrefix)
        {
            if (RuntimeDeckFactory.IsDeckValid(deck))
            {
                return deck;
            }

            Debug.LogWarning($"[CardBattle] {teamPrefix} 덱이 유효하지 않아 런타임 기본 덱으로 대체합니다.");
            return RuntimeDeckFactory.CreateDefaultDeck(teamPrefix);
        }
    }
}
