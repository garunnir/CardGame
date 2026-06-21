using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>보드 평면에 카드·영웅 면을 맞추는 앵커 회전.</summary>
    public static class CardBoardAnchorDefaults
    {
        public static readonly Quaternion FlatOnBoard = Quaternion.Euler(90f, 0f, 0f);
    }
}
