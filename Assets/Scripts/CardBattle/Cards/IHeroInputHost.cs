using System;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Cards
{
    public interface IHeroInputHost
    {
        HeroInstanceId InstanceId { get; }
        bool IsEnemyHero { get; }
        bool CanAcceptShortClick { get; }

        event Action<IHeroInputHost> ShortClicked;
        event Action<IHeroInputHost> LongPressed;
        event Action<IHeroInputHost> LongPressReleased;
    }
}
