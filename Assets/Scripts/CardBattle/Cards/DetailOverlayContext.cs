using System.Text;
using CardGame.CardBattle.Core;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    public enum DetailOverlayKind
    {
        Card,
        Hero,
    }

    public readonly struct DetailOverlayContext
    {
        public DetailOverlayContext(
            DetailOverlayKind kind,
            bool isRevealed,
            string displayName,
            Sprite cardSprite,
            int currentHp,
            int maxHp,
            int attackPower,
            string typeLabel,
            string contextLines,
            int currentShield,
            int currentMp,
            int maxMp)
        {
            Kind = kind;
            IsRevealed = isRevealed;
            DisplayName = displayName;
            CardSprite = cardSprite;
            CurrentHp = currentHp;
            MaxHp = maxHp;
            AttackPower = attackPower;
            TypeLabel = typeLabel;
            ContextLines = contextLines;
            CurrentShield = currentShield;
            CurrentMp = currentMp;
            MaxMp = maxMp;
        }

        public DetailOverlayKind Kind { get; }
        public bool IsRevealed { get; }
        public string DisplayName { get; }
        public Sprite CardSprite { get; }
        public int CurrentHp { get; }
        public int MaxHp { get; }
        public int AttackPower { get; }
        public string TypeLabel { get; }
        public string ContextLines { get; }
        public int CurrentShield { get; }
        public int CurrentMp { get; }
        public int MaxMp { get; }

        public static DetailOverlayContext FromCard(CardDetailContext cardContext)
        {
            return new DetailOverlayContext(
                DetailOverlayKind.Card,
                cardContext.IsRevealed,
                cardContext.DisplayName,
                cardContext.CardSprite,
                cardContext.CurrentHp,
                cardContext.MaxHp,
                cardContext.AttackPower,
                cardContext.TypeLabel,
                cardContext.ContextLines,
                0,
                0,
                0);
        }

        public static DetailOverlayContext FromCardModel(CardModel model, CardBoardPhase phase, Sprite cardBackSprite)
        {
            return FromCard(CardDetailContext.Build(model, phase, cardBackSprite));
        }

        public static DetailOverlayContext FromHero(HeroModel hero)
        {
            return FromHero(hero, null);
        }

        public static DetailOverlayContext FromHero(HeroModel hero, CardModel[] battlefield)
        {
            if (hero == null)
            {
                return default;
            }

            var behavior = hero.NormalAttackBehavior;
            var typeLabel = behavior != null && !string.IsNullOrWhiteSpace(behavior.displayName)
                ? behavior.displayName
                : "영웅";
            var description = BuildHeroDescription(hero);
            var contributions = BuildContributionsText(battlefield);
            var contextLines = description + "\n---\n" + contributions;

            return new DetailOverlayContext(
                DetailOverlayKind.Hero,
                isRevealed: true,
                displayName: hero.DisplayName,
                cardSprite: hero.Data != null ? hero.Data.portrait : null,
                currentHp: hero.CurrentHp,
                maxHp: hero.MaxHp,
                attackPower: hero.BaseAttack,
                typeLabel: typeLabel,
                contextLines: contextLines,
                currentShield: hero.CurrentShield,
                currentMp: hero.CurrentMp,
                maxMp: hero.MaxMp);
        }

        private static string BuildHeroDescription(HeroModel hero)
        {
            var attack = hero.NormalAttackBehavior;
            var shield = hero.ShieldBehavior;
            var attackName = attack != null ? attack.displayName : "평타";
            var shieldName = shield != null ? shield.displayName : "보호막";
            var attackDesc = attack != null ? attack.description : string.Empty;
            var shieldDesc = shield != null ? shield.description : string.Empty;

            return attackName + " / " + shieldName + "\n" + attackDesc + "\n" + shieldDesc;
        }

        private static string BuildContributionsText(CardModel[] battlefield)
        {
            var contributions = SlotSupportAggregator.PlanContributions(battlefield);
            if (contributions.Count == 0)
            {
                return "전장 기여 없음";
            }

            var builder = new StringBuilder();
            for (var i = 0; i < contributions.Count; i++)
            {
                var item = contributions[i];
                var card = item.SourceCard;
                var effect = item.Effect;
                if (i > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(card?.DisplayName ?? "?");
                AppendBonus(builder, effect.strikeBonus, "타격");
                AppendBonus(builder, effect.turnStartHeroHeal, "턴힐");
                AppendBonus(builder, effect.mpGainOnTurnStart, "턴MP");
                AppendBonus(builder, effect.mpGainOnHeroStrike, "타격MP");
                AppendBonus(builder, effect.defenseBonus, "방어");
            }

            return builder.ToString();
        }

        private static void AppendBonus(StringBuilder builder, int value, string label)
        {
            if (value == 0)
            {
                return;
            }

            builder.Append(' ');
            builder.Append(label);
            builder.Append(value > 0 ? "+" : string.Empty);
            builder.Append(value);
        }
    }
}
