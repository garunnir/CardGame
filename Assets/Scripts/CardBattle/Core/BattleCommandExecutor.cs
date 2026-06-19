using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Core
{
    /// <summary>전투 상태 변경 단일 진입점. 연출 전에 도메인을 먼저 적용.</summary>
    public static class BattleCommandExecutor
    {
        public static BattleActionResult ApplyAttack(AttackResolution resolution, BattleActionRequest request)
        {
            return BattleResolver.Apply(resolution, request.Attacker, request.Target);
        }
    }
}
