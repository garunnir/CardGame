using System;
using System.Collections.Generic;
using CardGame.CardBattle.Bridge;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using CardGame.CardBattle.Input;
using CardGame.CardBattle.Presentation;
using CardGame.CardBattle.States;
using CardGame.CardBattle.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.CardBattle.Core
{
    /// <summary>전투 FSM 및 데이터 허브. static 싱글톤 대신 주입/SerializeField 지원.</summary>
    public sealed class GameManager : MonoBehaviour, IBattleContext
    {
        [SerializeField] private UIManager uiManager;
        [SerializeField] private DragTargetingPresenter dragTargetingPresenter;
        [SerializeField] private CardDetailOverlayPresenter cardDetailOverlay;
        [SerializeField] private CardBoardPresenter cardBoardPresenter;
        [SerializeField] private HeroArenaPresenter heroArenaPresenter;

        private BaseState currentState;
        private int stateGeneration;
        private UnityInputProvider inputProvider = new UnityInputProvider();
        private CardPresentationService presentationService;
        private PresentationPlayer presentationPlayer;
        private BattleOrchestrator battleOrchestrator;
        private readonly HeroStrikeController heroStrikeController = new HeroStrikeController();
        private readonly BattleEffectBridge battleEffectBridge = new BattleEffectBridge();
        private CardInstanceId activeInspectCardId;
        private HeroInstanceId activeInspectHeroId;

        public BattleField Field { get; } = new BattleField();
        public HeroArenaField HeroArena { get; } = new HeroArenaField();
        public IInputProvider InputProvider => inputProvider;
        public List<CardDataAsset> PlayerDeckData { get; private set; } = new List<CardDataAsset>();
        public List<CardDataAsset> EnemyDeckData { get; private set; } = new List<CardDataAsset>();
        public HeroDataAsset PlayerHeroData { get; private set; }
        public HeroDataAsset EnemyHeroData { get; private set; }

        IReadOnlyList<CardDataAsset> IBattleContext.PlayerDeckData => PlayerDeckData;
        IReadOnlyList<CardDataAsset> IBattleContext.EnemyDeckData => EnemyDeckData;

        public BattleActionRequest PendingAction { get; set; }
        public bool IsPlayerTurn { get; set; }
        public BattleFlowStateId CurrentStateId => currentState?.StateId ?? BattleFlowStateId.Init;
        public int StateGeneration => stateGeneration;

        public Action<bool> OnBattleResult { get; set; }
        public event Action HeroTargetRequested;
        public DragTargetingPresenter DragTargetingPresenter => dragTargetingPresenter;

        private void Awake()
        {
            if (cardBoardPresenter != null)
            {
                cardBoardPresenter.InputHostsChanged += RefreshBoardInput;
            }

            inputProvider.CardLongPressed += OnCardLongPressed;
            inputProvider.CardLongPressReleased += OnCardLongPressReleased;
            inputProvider.HeroLongPressed += OnHeroLongPressed;
            inputProvider.HeroLongPressReleased += OnHeroLongPressReleased;

            ConfigureHeroInput();
        }

        private void OnDestroy()
        {
            inputProvider.CardLongPressed -= OnCardLongPressed;
            inputProvider.CardLongPressReleased -= OnCardLongPressReleased;
            inputProvider.HeroLongPressed -= OnHeroLongPressed;
            inputProvider.HeroLongPressReleased -= OnHeroLongPressReleased;

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

        public void ConfigureHeroPresenter(HeroArenaPresenter presenter)
        {
            heroArenaPresenter = presenter;
            ConfigureHeroInput();
        }

        private void ConfigureHeroInput()
        {
            if (heroArenaPresenter == null)
            {
                return;
            }

            heroArenaPresenter.BindEnemyHeroTarget(() => HeroTargetRequested?.Invoke());
            RefreshHeroInput();
        }

        public void ConfigurePresentation(BattleAudioAdapter audioAdapter)
        {
            var statFloatingText = cardBoardPresenter != null
                ? cardBoardPresenter.Layout?.statFloatingTextPresentation
                : null;
            presentationService = new CardPresentationService(audioAdapter, statFloatingText);
            presentationPlayer = new PresentationPlayer();
            battleOrchestrator = CreateOrchestrator();
        }

        public void InitializeBattle(
            List<CardDataAsset> playerDeck,
            List<CardDataAsset> enemyDeck,
            HeroDataAsset playerHero = null,
            HeroDataAsset enemyHero = null)
        {
            HideDetailOverlay();
            PlayerDeckData = SanitizeDeck(playerDeck, "Player");
            EnemyDeckData = SanitizeDeck(enemyDeck, "Enemy");
            PlayerHeroData = SanitizeHero(playerHero, "Player");
            EnemyHeroData = SanitizeHero(enemyHero, "Enemy");
            ChangeState(new InitState(this));
        }

        public void RestartBattle()
        {
            InitializeBattle(PlayerDeckData, EnemyDeckData, PlayerHeroData, EnemyHeroData);
        }

        public async UniTask<bool> ExecuteBattleAsync(BattleActionRequest request)
        {
            if (battleOrchestrator == null)
            {
                battleOrchestrator = CreateOrchestrator();
            }

            var result = await battleOrchestrator.ExecuteAsync(
                request,
                cardBoardPresenter,
                uiManager,
                heroArenaPresenter,
                onHeroStrike: _ => { },
                syncHeroViews: SyncHeroViews);

            if (cardBoardPresenter != null)
            {
                RefreshBoardInput();
            }

            RaiseReserveChanged();
            SetEnemyHeroTargetEnabled(false);

            if (!result.ContinueBattle)
            {
                ChangeState(new GameOverState(this, result.PlayerWon));
                return false;
            }

            return true;
        }

        public void ChangeState(BaseState nextState)
        {
            stateGeneration++;
            currentState?.Exit();
            currentState = nextState;
            currentState.Enter();
        }

        public bool IsStateGenerationCurrent(int generation) => generation == stateGeneration;

        public UniTask BuildBoardViewsAsync()
        {
            SyncHeroViews();
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
            SyncHeroViews();
            return SyncBoardViewsAsync(animateRefill: false);
        }

        public void SyncHeroViews()
        {
            heroArenaPresenter?.Refresh(HeroArena, Field);
            RefreshHeroInput();
        }

        public void SetEnemyHeroTargetEnabled(bool enabled)
        {
            heroArenaPresenter?.SetEnemyHeroTargetEnabled(enabled);
        }

        public bool CanTargetEnemyHero(CardModel attacker)
        {
            return CardTargetingRules.CanTargetEnemyHero(Field, HeroArena, attacker);
        }

        public void RequestHeroTarget()
        {
            HeroTargetRequested?.Invoke();
        }

        public IReadOnlyList<HeroSupportHealEvent> PlanHeroTurnStartEffects(bool isPlayerTurn)
        {
            return battleEffectBridge.PlanTurnStart(
                HeroArena,
                Field.PlayerBattlefield,
                Field.EnemyBattlefield,
                isPlayerTurn);
        }

        private async UniTask RunBoardPresentationAsync(UniTask boardTask, bool refreshInput)
        {
            await boardTask;
            if (refreshInput)
            {
                RefreshBoardInput();
            }

            UpdateReserveUi();
            SyncHeroViews();
        }

        private void RefreshBoardInput()
        {
            if (cardBoardPresenter != null)
            {
                inputProvider.BindInputHosts(cardBoardPresenter.InputHosts);
            }
        }

        private void RefreshHeroInput()
        {
            if (heroArenaPresenter == null)
            {
                inputProvider.BindHeroInputHosts(null, null);
                return;
            }

            inputProvider.BindHeroInputHosts(
                heroArenaPresenter.PlayerHeroInputHost,
                heroArenaPresenter.EnemyHeroInputHost);
        }

        private void UpdateReserveUi()
        {
            uiManager?.UpdateReserveCounts(
                Field.PlayerReserve.Count,
                Field.EnemyReserve.Count);
        }

        public void RaiseTurnBanner(bool isPlayerTurn)
        {
            uiManager?.ShowTurnBanner(isPlayerTurn);
        }

        public void RaiseSkipBanner(string message)
        {
            uiManager?.ShowSkipBanner(message);
        }

        public void RaiseGameOver(bool playerWin)
        {
            HideDetailOverlay();
            uiManager?.ShowResultPopup(playerWin);
        }

        public void RaiseReserveChanged()
        {
            UpdateReserveUi();
            SyncHeroViews();
        }

        public UniTask PlayTurnStartHealAsync(IReadOnlyList<TurnStartHealEvent> healEvents)
        {
            var events = TurnStartEffectAggregator.FromCardHealEvents(healEvents);
            return PlayTurnStartEffectsAsync(events);
        }

        public UniTask PlayHeroSupportEventsAsync(IReadOnlyList<HeroSupportHealEvent> events)
        {
            var unified = TurnStartEffectAggregator.FromHeroSupportEvents(events);
            return PlayTurnStartEffectsAsync(unified);
        }

        public UniTask PlayTurnStartEffectsAsync(IReadOnlyList<TurnStartEffectEvent> events)
        {
            if (events == null || events.Count == 0)
            {
                return UniTask.CompletedTask;
            }

            return PlayTurnStartPresentationAsync(events);
        }

        private async UniTask PlayTurnStartPresentationAsync(IReadOnlyList<TurnStartEffectEvent> events)
        {
            ICardViewRegistry viewRegistry = cardBoardPresenter;
            var planInputs = TurnStartPresentationPlanInput.FromEvents(events);

            if (presentationPlayer == null)
            {
                presentationService?.PlayTurnStartEffectsImmediate(
                    planInputs,
                    FindView,
                    heroArenaPresenter);
                SyncTurnStartTargets(events);
                return;
            }

            var sequence = PresentationSequenceBuilder.BuildTurnStartEffects(events);
            await presentationPlayer.PlayTurnStartAsync(
                sequence,
                uiManager,
                presentationService,
                viewRegistry,
                heroArenaPresenter);
            SyncTurnStartTargets(events);
        }

        private void SyncTurnStartTargets(IReadOnlyList<TurnStartEffectEvent> events)
        {
            if (cardBoardPresenter == null)
            {
                SyncHeroViews();
                return;
            }

            for (var i = 0; i < events.Count; i++)
            {
                var target = events[i].TargetCard;
                if (target == null)
                {
                    continue;
                }

                if (cardBoardPresenter.TryGetView(target.InstanceId, out var view))
                {
                    view.SetHpDisplay(target.CurrentHp);
                }
            }

            SyncHeroViews();
        }

        public ICardBattleView FindView(CardInstanceId id)
        {
            if (cardBoardPresenter != null && cardBoardPresenter.TryGetView(id, out var view))
            {
                return view;
            }

            return null;
        }

        public bool TryGetModel(CardInstanceId id, out CardModel model)
        {
            model = null;
            return cardBoardPresenter != null && cardBoardPresenter.TryGetModel(id, out model);
        }

        private void OnCardLongPressed(CardInstanceId cardId)
        {
            if (!TryGetModel(cardId, out var model))
            {
                return;
            }

            var phase = CardBoardPhase.Hidden;
            if (cardBoardPresenter != null && cardBoardPresenter.TryGetEntity(cardId, out var entity))
            {
                phase = entity.Phase;
            }

            var layout = cardBoardPresenter != null ? cardBoardPresenter.Layout : null;
            var cardBack = layout != null ? layout.GetCardBack(model.IsPlayerTeam) : null;
            ShowDetailOverlay(DetailOverlayContext.FromCardModel(model, phase, cardBack));
            activeInspectCardId = cardId;
            activeInspectHeroId = default;
        }

        private void OnCardLongPressReleased(CardInstanceId cardId)
        {
            if (!activeInspectCardId.IsValid || activeInspectCardId != cardId)
            {
                return;
            }

            HideDetailOverlay();
        }

        private void OnHeroLongPressed(HeroInstanceId heroId)
        {
            var hero = ResolveHero(heroId);
            if (hero == null)
            {
                return;
            }

            var field = hero.IsPlayerTeam ? Field.PlayerBattlefield : Field.EnemyBattlefield;
            ShowDetailOverlay(DetailOverlayContext.FromHero(hero, field));
            activeInspectHeroId = heroId;
            activeInspectCardId = default;
        }

        private void OnHeroLongPressReleased(HeroInstanceId heroId)
        {
            if (!activeInspectHeroId.IsValid || activeInspectHeroId != heroId)
            {
                return;
            }

            HideDetailOverlay();
        }

        private HeroModel ResolveHero(HeroInstanceId heroId)
        {
            if (HeroArena.PlayerHero != null && HeroArena.PlayerHero.InstanceId == heroId)
            {
                return HeroArena.PlayerHero;
            }

            if (HeroArena.EnemyHero != null && HeroArena.EnemyHero.InstanceId == heroId)
            {
                return HeroArena.EnemyHero;
            }

            return heroArenaPresenter?.FindHero(heroId);
        }

        private void ShowDetailOverlay(DetailOverlayContext context)
        {
            cardDetailOverlay?.Show(context);
        }

        private void HideDetailOverlay()
        {
            cardDetailOverlay?.Hide();
            activeInspectCardId = default;
            activeInspectHeroId = default;
        }

        private void HideCardDetailOverlay() => HideDetailOverlay();

        private BattleOrchestrator CreateOrchestrator()
        {
            return new BattleOrchestrator(
                Field,
                HeroArena,
                heroStrikeController,
                presentationPlayer,
                presentationService,
                ResolveAttackPresentationTailDelay());
        }

        private float ResolveAttackPresentationTailDelay()
        {
            var layout = cardBoardPresenter != null ? cardBoardPresenter.Layout : null;
            return layout != null ? layout.attackPresentationTailDelay : 0.55f;
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

        private static HeroDataAsset SanitizeHero(HeroDataAsset hero, string teamPrefix)
        {
            if (hero != null
                && hero.normalAttackBehavior != null
                && hero.shieldBehavior != null)
            {
                return hero;
            }

            Debug.LogWarning($"[CardBattle] {teamPrefix} 영웅 데이터가 없어 런타임 기본 영웅으로 대체합니다.");
            return RuntimeDeckFactory.CreateDefaultHero(teamPrefix);
        }
    }
}
