using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>행동 SO 데이터 → 연출 모듈. Cards 레이어가 Presentation 구현체를 참조하지 않도록 역전.</summary>
    public static class PresentationModuleFactory
    {
        public static void CollectModules(CardBehaviorAsset behavior, IPresentationModuleCollector collector)
        {
            if (behavior == null || collector == null)
            {
                return;
            }

            switch (behavior)
            {
                case NormalBehaviorAsset normal:
                    CollectNormal(normal, collector);
                    break;
                case RangedBehaviorAsset ranged:
                    CollectRanged(ranged, collector);
                    break;
                case MusouBehaviorAsset musou:
                    CollectMusou(musou, collector);
                    break;
                case HealerBehaviorAsset healer:
                    CollectHealer(healer, collector);
                    break;
            }
        }

        private static void CollectNormal(NormalBehaviorAsset behavior, IPresentationModuleCollector collector)
        {
            var presentation = behavior.presentation;
            collector.Add(new MeleeAttackPresentationModule(
                presentation.attackDashDuration,
                presentation.hitShakeStrength));
            collector.Add(new CounterPresentationModule());
            collector.Add(new DefaultCameraShakePresentationModule());
        }

        private static void CollectRanged(RangedBehaviorAsset behavior, IPresentationModuleCollector collector)
        {
            collector.Add(new RangedAttackPresentationModule(behavior.presentation.shootDuration));
            collector.Add(new DefaultCameraShakePresentationModule());
        }

        private static void CollectMusou(MusouBehaviorAsset behavior, IPresentationModuleCollector collector)
        {
            var presentation = behavior.presentation;
            collector.Add(new MeleeAttackPresentationModule(presentation.attackDashDuration, 0f));
            collector.Add(new CounterPresentationModule());
            collector.Add(new MusouSecondaryPresentationModule(
                presentation.secondaryHitDelay,
                presentation.secondaryCameraShake));
        }

        private static void CollectHealer(HealerBehaviorAsset behavior, IPresentationModuleCollector collector)
        {
            collector.Add(new MeleeAttackPresentationModule(0f, 0f));
            collector.Add(new CounterPresentationModule());
            collector.Add(new DefaultCameraShakePresentationModule());
        }
    }
}
