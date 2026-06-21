using CardGame.CardBattle.Core;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>영웅 뷰 표시 DTO.</summary>
    public readonly struct HeroViewState
    {
        public HeroViewState(
            HeroInstanceId instanceId,
            string displayName,
            Sprite portrait,
            int maxHp,
            int maxMp,
            int displayHp,
            int displayShield,
            int displayMp,
            bool isPlayerTeam,
            bool isAlive)
        {
            InstanceId = instanceId;
            DisplayName = displayName;
            Portrait = portrait;
            MaxHp = maxHp;
            MaxMp = maxMp;
            DisplayHp = displayHp;
            DisplayShield = displayShield;
            DisplayMp = displayMp;
            IsPlayerTeam = isPlayerTeam;
            IsAlive = isAlive;
        }

        public HeroInstanceId InstanceId { get; }
        public string DisplayName { get; }
        public Sprite Portrait { get; }
        public int MaxHp { get; }
        public int MaxMp { get; }
        public int DisplayHp { get; }
        public int DisplayShield { get; }
        public int DisplayMp { get; }
        public bool IsPlayerTeam { get; }
        public bool IsAlive { get; }

        public bool IsValid => InstanceId.IsValid;

        public static HeroViewState FromModel(HeroModel model)
        {
            if (model == null)
            {
                return default;
            }

            return new HeroViewState(
                model.InstanceId,
                model.DisplayName,
                model.Data != null ? model.Data.portrait : null,
                model.MaxHp,
                model.MaxMp,
                model.CurrentHp,
                model.CurrentShield,
                model.CurrentMp,
                model.IsPlayerTeam,
                model.IsAlive);
        }
    }
}
