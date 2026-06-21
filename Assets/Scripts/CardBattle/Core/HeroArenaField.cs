using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Core
{
    public sealed class HeroArenaField
    {
        public HeroModel PlayerHero { get; private set; }
        public HeroModel EnemyHero { get; private set; }

        public void Clear()
        {
            PlayerHero = null;
            EnemyHero = null;
        }

        public void DeployInitial(HeroDataAsset playerHero, HeroDataAsset enemyHero)
        {
            PlayerHero = playerHero != null ? new HeroModel(playerHero, true) : null;
            EnemyHero = enemyHero != null ? new HeroModel(enemyHero, false) : null;
        }

        public HeroModel GetHero(bool isPlayerTeam)
        {
            return isPlayerTeam ? PlayerHero : EnemyHero;
        }

        public HeroModel GetOpponentHero(bool isPlayerTeam)
        {
            return isPlayerTeam ? EnemyHero : PlayerHero;
        }

        public bool IsHeroDefeated(bool isPlayerTeam)
        {
            var hero = GetHero(isPlayerTeam);
            return hero == null || !hero.IsAlive;
        }

        public bool IsEitherHeroDefeated =>
            IsHeroDefeated(true) || IsHeroDefeated(false);
    }
}
