using System;
using System.Collections.Generic;
using CardGame.CardBattle.Bridge;
using CardGame.CardBattle.Cards;
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
        [SerializeField] private CardView[] playerCardViews = new CardView[BattleField.SlotCount];
        [SerializeField] private CardView[] enemyCardViews = new CardView[BattleField.SlotCount];
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

        private void Awake()
        {
            inputProvider.BindViews(playerCardViews);
            inputProvider.BindViews(enemyCardViews);
        }

        public void ConfigurePresentation(BattleAudioAdapter audioAdapter)
        {
            presentationService = new CardPresentationService(audioAdapter);
            presentationPlayer = new PresentationPlayer();
        }

        /// <summary>로비/외부 덱 데이터로 전투 시작.</summary>
        public void InitializeBattle(List<CardDataAsset> playerDeck, List<CardDataAsset> enemyDeck)
        {
            PlayerDeckData = playerDeck ?? new List<CardDataAsset>();
            EnemyDeckData = enemyDeck ?? new List<CardDataAsset>();
            ChangeState(new InitState(this));
        }

        public void RestartBattle()
        {
            InitializeBattle(PlayerDeckData, EnemyDeckData);
        }

        /// <summary>공격 연산 + 연출 + 사망/승패. true = 전투 계속.</summary>
        public async UniTask<bool> ExecuteBattleAsync(BattleActionRequest request)
        {
            if (request.Attacker == null || request.Target == null)
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

            var result = presentationPlayer != null
                ? await presentationPlayer.PlayAttackAsync(presentationContext, sequence)
                : BattleResolver.Apply(resolution, request.Attacker, request.Target);

            RaiseBattleResolved(result);

            Field.ProcessDeathsAndRefill(true);
            Field.ProcessDeathsAndRefill(false);
            SyncAllViews();
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

        public void SyncAllViews()
        {
            SyncTeamViews(Field.PlayerBattlefield, playerCardViews);
            SyncTeamViews(Field.EnemyBattlefield, enemyCardViews);
            uiManager?.UpdateReserveCounts(
                Field.PlayerReserve.Count,
                Field.EnemyReserve.Count);
        }

        private static void SyncTeamViews(CardModel[] models, CardView[] views)
        {
            for (var i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view == null)
                {
                    continue;
                }

                var model = i < models.Length ? models[i] : null;
                if (model != null && model.IsAlive)
                {
                    view.gameObject.SetActive(true);
                    view.Bind(model);
                }
                else
                {
                    view.gameObject.SetActive(false);
                }
            }
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

        public CardView FindView(CardModel model)
        {
            if (model == null)
            {
                return null;
            }

            var views = model.IsPlayerTeam ? playerCardViews : enemyCardViews;
            for (var i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view != null && view.BoundModel == model)
                {
                    return view;
                }
            }

            return null;
        }
    }
}
