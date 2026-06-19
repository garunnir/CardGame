using System;
using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Input;
using Cysharp.Threading.Tasks;

namespace CardGame.CardBattle.Core
{
    /// <summary>FSM 상태가 GameManager 구현체에 직접 묶이지 않도록 하는 전투 컨텍스트.</summary>
    public interface IBattleContext
    {
        BattleField Field { get; }
        IInputProvider InputProvider { get; }
        IReadOnlyList<CardDataAsset> PlayerDeckData { get; }
        IReadOnlyList<CardDataAsset> EnemyDeckData { get; }

        BattleActionRequest PendingAction { get; set; }
        bool IsPlayerTurn { get; set; }
        BattleFlowStateId CurrentStateId { get; }
        int StateGeneration { get; }

        DragTargetingPresenter DragTargetingPresenter { get; }

        Action<bool> OnBattleResult { get; set; }

        void ChangeState(BaseState nextState);
        bool IsStateGenerationCurrent(int generation);

        UniTask BuildBoardViewsAsync();
        UniTask SyncAllViewsAsync();
        UniTask<bool> ExecuteBattleAsync(BattleActionRequest request);

        void RaiseTurnBanner(bool isPlayerTurn);
        void RaiseHealerPulse(IReadOnlyList<TurnStartHealEvent> healEvents);
        void RaiseReserveChanged();
        void RaiseGameOver(bool playerWin);

        ICardBattleView FindView(CardInstanceId id);
        bool TryGetModel(CardInstanceId id, out CardModel model);
    }
}
