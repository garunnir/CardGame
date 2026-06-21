using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>행동 SO 데이터 → 연출 모듈. Cards 레이어가 Presentation 구현체를 참조하지 않도록 역전.</summary>
    public static class PresentationModuleFactory
    {
        public static void CollectModules(BattlePresentationSpec spec, IPresentationModuleCollector collector)
        {
            if (spec == null || collector == null)
            {
                return;
            }

            switch (spec.Kind)
            {
                case PresentationKind.CardVsCard:
                    CollectCardModules(spec.CardBehavior, collector);
                    break;
                case PresentationKind.CardVsHero:
                    CollectCardVsHeroModules(spec.CardBehavior, collector);
                    break;
                case PresentationKind.HeroStrike:
                    CollectHeroStrikeModules(spec.HeroBehavior, spec.HeroStrikeResult, collector);
                    break;
            }
        }

        public static void CollectCardModules(CardBehaviorAsset behavior, IPresentationModuleCollector collector)
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

        private static void CollectCardVsHeroModules(CardBehaviorAsset behavior, IPresentationModuleCollector collector)
        {
            if (behavior == null)
            {
                return;
            }

            switch (behavior)
            {
                case RangedBehaviorAsset _:
                    collector.Add(new RangedCardVsHeroPresentationModule());
                    collector.Add(new HeroCardCounterPresentationModule());
                    collector.Add(new DefaultCameraShakePresentationModule());
                    break;
                default:
                    var dash = behavior is NormalBehaviorAsset normal
                        ? normal.presentation.attackDashDuration
                        : behavior is MusouBehaviorAsset musou
                            ? musou.presentation.attackDashDuration
                            : behavior is HealerBehaviorAsset healer
                                ? 0f
                                : 0.25f;
                    var shake = behavior is NormalBehaviorAsset normal2
                        ? normal2.presentation.hitShakeStrength
                        : 0.12f;
                    collector.Add(new CardVsHeroMeleePresentationModule(dash, shake));
                    collector.Add(new HeroCardCounterPresentationModule());
                    collector.Add(new DefaultCameraShakePresentationModule());
                    break;
            }
        }

        private static void CollectHeroStrikeModules(
            HeroBehaviorAsset behavior,
            HeroStrikeResult result,
            IPresentationModuleCollector collector)
        {
            if (result.UsedShield && behavior is HeroShieldBehaviorAsset shield)
            {
                collector.Add(new HeroShieldBuffPresentationModule(shield.presentation.buffBloomIntensity));
                return;
            }

            if (behavior is HeroNormalAttackBehaviorAsset normal)
            {
                var presentation = normal.presentation;
                collector.Add(new HeroStrikePresentationModule(
                    presentation.strikeDashDuration,
                    presentation.hitShakeStrength,
                    presentation.cameraShake));
            }
            else
            {
                collector.Add(new HeroStrikePresentationModule(0.25f, 0.12f, 0.12f));
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
            collector.Add(new RangedAttackPresentationModule());
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
