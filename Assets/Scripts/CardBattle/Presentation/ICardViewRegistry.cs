using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>CardInstanceId → 뷰/모델 조회. 연출·입력 해석의 공통 레지스트리.</summary>
    public interface ICardViewRegistry
    {
        bool TryGetView(CardInstanceId id, out ICardBattleView view);

        bool TryGetModel(CardInstanceId id, out CardModel model);
    }
}
