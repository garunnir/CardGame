using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>behavior SO 미연결 시 런타임 폴백 제공.</summary>
    public static class CardBehaviorLibrary
    {
        private static readonly CardBehaviorAsset[] RuntimeDefaults =
        {
            null,
            null,
            null,
            null
        };

        public static CardBehaviorAsset Resolve(CardBehaviorAsset behavior, CardType fallbackType = CardType.Normal)
        {
            if (behavior != null)
            {
                return behavior;
            }

            return GetRuntimeDefault(fallbackType);
        }

        public static CardBehaviorAsset GetRuntimeDefault(CardType type)
        {
            var index = (int)type;
            if (index < 0 || index >= RuntimeDefaults.Length)
            {
                type = CardType.Normal;
                index = 0;
            }

            if (RuntimeDefaults[index] != null)
            {
                return RuntimeDefaults[index];
            }

            CardBehaviorAsset asset;
            switch (type)
            {
                case CardType.Ranged:
                    asset = ScriptableObject.CreateInstance<RangedBehaviorAsset>();
                    ((RangedBehaviorAsset)asset).presentation = new RangedBehaviorPresentation();
                    break;
                case CardType.Musou:
                    asset = ScriptableObject.CreateInstance<MusouBehaviorAsset>();
                    ((MusouBehaviorAsset)asset).presentation = new MusouBehaviorPresentation();
                    break;
                case CardType.Healer:
                    asset = ScriptableObject.CreateInstance<HealerBehaviorAsset>();
                    ((HealerBehaviorAsset)asset).presentation = new HealerBehaviorPresentation();
                    break;
                default:
                    asset = ScriptableObject.CreateInstance<NormalBehaviorAsset>();
                    ((NormalBehaviorAsset)asset).presentation = new NormalBehaviorPresentation();
                    break;
            }

            asset.behaviorId = $"runtime_{type}";
            RuntimeDefaults[index] = asset;
            return asset;
        }
    }
}
