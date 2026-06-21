using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>CardInstanceId → 뷰 조회. TryGetModel은 입력/UI 빌드 전용 — 연출 Playback에서 사용 금지.</summary>
    public interface ICardViewRegistry
    {
        bool TryGetView(CardInstanceId id, out ICardBattleView view);

        bool TryGetModel(CardInstanceId id, out CardModel model);
    }
}
