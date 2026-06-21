using System;
using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Input;
using CardGame.CardBattle.Presentation;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.CardBattle.Core
{
    /// <summary>FSM 상태가 GameManager 구현체에 직접 묶이지 않도록 하는 전투 컨텍스트.</summary>
    public interface IBattleContext
    {
        BattleField Field { get; }
        HeroArenaField HeroArena { get; }
        IInputProvider InputProvider { get; }
        IReadOnlyList<CardDataAsset> PlayerDeckData { get; }
        IReadOnlyList<CardDataAsset> EnemyDeckData { get; }
        HeroDataAsset PlayerHeroData { get; }
        HeroDataAsset EnemyHeroData { get; }

        BattleActionRequest PendingAction { get; set; }
        bool IsPlayerTurn { get; set; }
        BattleFlowStateId CurrentStateId { get; }
        int StateGeneration { get; }

        DragTargetingPresenter DragTargetingPresenter { get; }

        Action<bool> OnBattleResult { get; set; }
        event Action HeroTargetRequested;

        void ChangeState(BaseState nextState);
        bool IsStateGenerationCurrent(int generation);

        UniTask BuildBoardViewsAsync();
        UniTask SyncAllViewsAsync();
        UniTask<bool> ExecuteBattleAsync(BattleActionRequest request);
        UniTask<bool> ExecuteHeroStrikeTurnAsync(bool isPlayerTeam);

        void RaiseTurnBanner(bool isPlayerTurn);
        void RaiseSkipBanner(string message);
        UniTask PlayTurnStartHealAsync(IReadOnlyList<TurnStartHealEvent> healEvents);
        UniTask PlayHeroSupportEventsAsync(IReadOnlyList<HeroSupportHealEvent> events);
        UniTask PlayTurnStartEffectsAsync(IReadOnlyList<TurnStartEffectEvent> events);
        IReadOnlyList<HeroSupportHealEvent> PlanHeroTurnStartEffects(bool isPlayerTurn);
        void SyncHeroViews();
        void SetEnemyHeroTargetEnabled(bool enabled);
        void RaiseReserveChanged();
        void RaiseGameOver(bool playerWin);
        void RequestHeroTarget();

        ICardBattleView FindView(CardInstanceId id);
        bool TryGetModel(CardInstanceId id, out CardModel model);
        bool CanTargetEnemyHero(CardModel attacker);
        bool IsPointerOverEnemyHero(Vector2 screenPosition);
    }
}
