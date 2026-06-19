namespace CardGame.CardBattle.Core
{
    /// <summary>기본 덱 카드 ID 단일 진실원. Bridge Inspector 배열은 테스트용 오버라이드.</summary>
    public static class DefaultDeckCatalog
    {
        public static readonly string[] PlayerCardIds =
        {
            "Player_Normal_01",
            "Player_Normal_02",
            "Player_Musou_01",
            "Player_Healer_01",
            "Player_Ranged_01",
            "Player_Ranged_02"
        };

        public static readonly string[] EnemyCardIds =
        {
            "Enemy_Normal_01",
            "Enemy_Normal_02",
            "Enemy_Musou_01",
            "Enemy_Healer_01",
            "Enemy_Ranged_01",
            "Enemy_Ranged_02"
        };

        public static string[] GetCardIdsForTeam(string teamPrefix)
        {
            return teamPrefix == "Player" ? PlayerCardIds : EnemyCardIds;
        }
    }
}
