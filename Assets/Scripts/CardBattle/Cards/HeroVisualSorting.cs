namespace CardGame.CardBattle.Cards
{
    /// <summary>영웅 3D 카드 렌더 순서 — 바는 초상 뒤, 라벨은 앞.</summary>
    public static class HeroVisualSorting
    {
        public const float BarLocalZ = 0.006f;
        public const float PortraitLocalZ = CardFaceView.FrontFaceLocalZ;

        public const int BarBackground = 0;
        public const int BarFill = 1;
        public const int Portrait = 5;
        public const int NameLabel = 6;
        public const int StatLabel = 7;
        public const int FloatingText = 10;
    }
}
