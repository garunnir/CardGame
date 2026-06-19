using CardGame.CardBattle.Core;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>뷰에 필요한 표시 데이터만 담는 DTO. 도메인 CardModel과 분리.</summary>
    public readonly struct CardViewState
    {
        public CardViewState(
            CardInstanceId instanceId,
            string displayName,
            Sprite illustration,
            int maxHp,
            int displayHp,
            bool isPlayerTeam,
            bool isAlive)
        {
            InstanceId = instanceId;
            DisplayName = displayName;
            Illustration = illustration;
            MaxHp = maxHp;
            DisplayHp = displayHp;
            IsPlayerTeam = isPlayerTeam;
            IsAlive = isAlive;
        }

        public CardInstanceId InstanceId { get; }
        public string DisplayName { get; }
        public Sprite Illustration { get; }
        public int MaxHp { get; }
        public int DisplayHp { get; }
        public bool IsPlayerTeam { get; }
        public bool IsAlive { get; }

        public bool IsValid => InstanceId.IsValid;

        public static CardViewState FromModel(CardModel model)
        {
            if (model == null)
            {
                return default;
            }

            return new CardViewState(
                model.InstanceId,
                model.DisplayName,
                CardVisualDefaults.ResolveIllustration(model.Data),
                model.MaxHp,
                model.CurrentHp,
                model.IsPlayerTeam,
                model.IsAlive);
        }
    }
}
