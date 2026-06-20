using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>롱프레스 카드 상세 오버레이 표시용 DTO.</summary>
    public readonly struct CardDetailContext
    {
        public CardDetailContext(
            bool isRevealed,
            string displayName,
            Sprite cardSprite,
            int currentHp,
            int maxHp,
            int attackPower,
            string typeLabel,
            string contextLines)
        {
            IsRevealed = isRevealed;
            DisplayName = displayName;
            CardSprite = cardSprite;
            CurrentHp = currentHp;
            MaxHp = maxHp;
            AttackPower = attackPower;
            TypeLabel = typeLabel;
            ContextLines = contextLines;
        }

        public bool IsRevealed { get; }
        public string DisplayName { get; }
        public Sprite CardSprite { get; }
        public int CurrentHp { get; }
        public int MaxHp { get; }
        public int AttackPower { get; }
        public string TypeLabel { get; }
        public string ContextLines { get; }

        public static CardDetailContext Build(CardModel model, CardBoardPhase phase, Sprite cardBackSprite)
        {
            if (model == null)
            {
                return default;
            }

            var behavior = model.Behavior;

            if (phase != CardBoardPhase.BattlefieldFaceUp)
            {
                return new CardDetailContext(
                    isRevealed: false,
                    displayName: ResolveHiddenLabel(behavior),
                    cardSprite: cardBackSprite,
                    currentHp: 0,
                    maxHp: 0,
                    attackPower: 0,
                    typeLabel: string.Empty,
                    contextLines: ResolveHiddenDescription(behavior));
            }

            return new CardDetailContext(
                isRevealed: true,
                displayName: model.DisplayName,
                cardSprite: CardVisualDefaults.ResolveIllustration(model.Data),
                currentHp: model.CurrentHp,
                maxHp: model.MaxHp,
                attackPower: model.AttackPower,
                typeLabel: ResolveTypeLabel(model),
                contextLines: ResolveContextLines(model));
        }

        private static string ResolveTypeLabel(CardModel model)
        {
            var data = model.Data;
            if (data != null && !string.IsNullOrWhiteSpace(data.detailTypeLabelOverride))
            {
                return data.detailTypeLabelOverride.Trim();
            }

            var behavior = model.Behavior;
            if (behavior != null && !string.IsNullOrWhiteSpace(behavior.detailTypeLabel))
            {
                return behavior.detailTypeLabel.Trim();
            }

            return CardDetailContextFallback.TypeLabelFor(model.CardType);
        }

        private static string ResolveContextLines(CardModel model)
        {
            var data = model.Data;
            if (data != null && !string.IsNullOrWhiteSpace(data.detailDescriptionOverride))
            {
                return data.detailDescriptionOverride.Trim();
            }

            var behavior = model.Behavior;
            if (behavior != null && !string.IsNullOrWhiteSpace(behavior.detailDescription))
            {
                return behavior.detailDescription.Trim();
            }

            return CardDetailContextFallback.DescriptionFor(model);
        }

        private static string ResolveHiddenLabel(CardBehaviorAsset behavior)
        {
            if (behavior != null && !string.IsNullOrWhiteSpace(behavior.hiddenDetailLabel))
            {
                return behavior.hiddenDetailLabel.Trim();
            }

            return "???";
        }

        private static string ResolveHiddenDescription(CardBehaviorAsset behavior)
        {
            if (behavior != null && !string.IsNullOrWhiteSpace(behavior.hiddenDetailDescription))
            {
                return behavior.hiddenDetailDescription.Trim();
            }

            return "알 수 없음";
        }

        /// <summary>Behavior SO 상세 필드가 비어 있을 때 타입별 기본 문구를 채웁니다.</summary>
        public static void EnsureBehaviorDetailDefaults(CardBehaviorAsset behavior, bool onlyIfEmpty = true)
        {
            CardDetailContextFallback.ApplyDefaultDetailFields(behavior, !onlyIfEmpty);
        }
    }

    internal static class CardDetailContextFallback
    {
        public static string TypeLabelFor(CardType type)
        {
            switch (type)
            {
                case CardType.Ranged:
                    return "원거리";
                case CardType.Musou:
                    return "무쌍";
                case CardType.Healer:
                    return "힐러";
                default:
                    return "일반";
            }
        }

        public static string DescriptionFor(CardModel model)
        {
            if (model == null)
            {
                return string.Empty;
            }

            return DescriptionForBehavior(model.Behavior);
        }

        public static string DescriptionForBehavior(CardBehaviorAsset behavior)
        {
            if (behavior == null)
            {
                return string.Empty;
            }

            switch (behavior)
            {
                case RangedBehaviorAsset _:
                    return "현재 HP만큼 피해\n반격 없음";
                case MusouBehaviorAsset musou:
                    var ratio = musou.secondaryDamageRatio;
                    var musouCounter = musou.receivesCounterAttack;
                    return "주 타겟 + 인접 1명 추가 피해 (" + ratio.ToString("0.#") + "배)\n"
                        + (musouCounter ? "반격 받음" : "반격 없음");
                case HealerBehaviorAsset healer:
                    var healLine = "턴 시작 아군 회복 (+" + healer.turnHealAmount + ")";
                    if (healer.excludesSelf)
                    {
                        healLine += ", 자신 제외";
                    }

                    return healLine + "\n공격 가능 · " + (healer.receivesCounterAttack ? "반격 받음" : "반격 없음");
                case NormalBehaviorAsset normal:
                    return "현재 HP만큼 피해\n" + (normal.receivesCounterAttack ? "반격 받음" : "반격 없음");
                default:
                    return "현재 HP만큼 피해\n반격 받음";
            }
        }

        public static void ApplyDefaultDetailFields(CardBehaviorAsset behavior, bool overwriteExisting)
        {
            if (behavior == null)
            {
                return;
            }

            if (overwriteExisting || string.IsNullOrWhiteSpace(behavior.detailTypeLabel))
            {
                behavior.detailTypeLabel = TypeLabelFor(behavior.StrategyType);
            }

            if (overwriteExisting || string.IsNullOrWhiteSpace(behavior.detailDescription))
            {
                behavior.detailDescription = DescriptionForBehavior(behavior);
            }

            if (overwriteExisting || string.IsNullOrWhiteSpace(behavior.hiddenDetailLabel))
            {
                behavior.hiddenDetailLabel = "???";
            }

            if (overwriteExisting || string.IsNullOrWhiteSpace(behavior.hiddenDetailDescription))
            {
                behavior.hiddenDetailDescription = "알 수 없음";
            }
        }
    }
}
