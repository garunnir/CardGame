using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    internal static class BehaviorDamageMath
    {
        public static int ScalePrimary(float multiplier, int attackPower)
        {
            if (multiplier <= 0f)
            {
                return 0;
            }

            return Mathf.Max(1, Mathf.RoundToInt(attackPower * multiplier));
        }

        public static int ScaleSecondary(float ratio, int attackPower)
        {
            if (ratio <= 0f)
            {
                return 0;
            }

            return Mathf.Max(1, Mathf.RoundToInt(attackPower * ratio));
        }
    }
}
