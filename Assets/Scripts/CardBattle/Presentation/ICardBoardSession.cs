using System;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>보드 연출 lock·동기화. Orchestrator가 concrete presenter에 의존하지 않도록 분리.</summary>
    public interface ICardBoardSession
    {
        UniTask RunExclusiveAsync(Func<UniTask> work);

        UniTask SyncBoardWithinLockAsync(BattleField field, bool animateRefill);
    }
}
