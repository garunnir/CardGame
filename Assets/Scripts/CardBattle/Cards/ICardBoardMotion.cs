using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>보드 앵커 배치 연출 — reparent·deploy·realign·reserve 스냅.</summary>
    public interface ICardBoardMotion
    {
        UniTask ApplyPlacement(
            Transform anchorParent,
            Vector3 targetLocalPosition,
            Quaternion targetLocalRotation,
            CardBoardPhase targetPhase,
            bool animate);

        void CancelMotion();
    }
}
