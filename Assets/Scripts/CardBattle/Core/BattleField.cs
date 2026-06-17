using System;
using System.Collections.Generic;
using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Core
{
    /// <summary>전장/대기열 및 승패 판정 데이터.</summary>
    public sealed class BattleField
    {
        public const int SlotCount = 3;

        public CardModel[] PlayerBattlefield { get; } = new CardModel[SlotCount];
        public CardModel[] EnemyBattlefield { get; } = new CardModel[SlotCount];
        public List<CardModel> PlayerReserve { get; } = new List<CardModel>(SlotCount);
        public List<CardModel> EnemyReserve { get; } = new List<CardModel>(SlotCount);

        public int PlayerRemainingCount => CountAlive(PlayerBattlefield) + PlayerReserve.Count;
        public int EnemyRemainingCount => CountAlive(EnemyBattlefield) + EnemyReserve.Count;

        public void Clear()
        {
            for (var i = 0; i < SlotCount; i++)
            {
                PlayerBattlefield[i] = null;
                EnemyBattlefield[i] = null;
            }

            PlayerReserve.Clear();
            EnemyReserve.Clear();
        }

        public CardModel[] GetBattlefield(bool isPlayerTeam)
        {
            return isPlayerTeam ? PlayerBattlefield : EnemyBattlefield;
        }

        public List<CardModel> GetReserve(bool isPlayerTeam)
        {
            return isPlayerTeam ? PlayerReserve : EnemyReserve;
        }

        public bool IsTeamDefeated(bool isPlayerTeam)
        {
            return isPlayerTeam ? PlayerRemainingCount <= 0 : EnemyRemainingCount <= 0;
        }

        /// <summary>사망 처리 후 대기 카드 자동 전진. 제거된 카드 수 반환.</summary>
        public int ProcessDeathsAndRefill(bool isPlayerTeam)
        {
            var field = GetBattlefield(isPlayerTeam);
            var reserve = GetReserve(isPlayerTeam);
            var removed = 0;

            for (var i = 0; i < SlotCount; i++)
            {
                var card = field[i];
                if (card == null || card.IsAlive)
                {
                    continue;
                }

                removed++;
                field[i] = null;

                if (reserve.Count > 0)
                {
                    var next = reserve[0];
                    reserve.RemoveAt(0);
                    next.SlotIndex = i;
                    field[i] = next;
                }
            }

            return removed;
        }

        public static int CountAlive(CardModel[] field)
        {
            var count = 0;
            for (var i = 0; i < field.Length; i++)
            {
                if (field[i] != null && field[i].IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        public void DeployInitial(List<CardDataAsset> deck, bool isPlayerTeam)
        {
            if (deck == null || deck.Count < SlotCount * 2)
            {
                throw new ArgumentException("덱은 최소 6장이 필요합니다.");
            }

            var field = GetBattlefield(isPlayerTeam);
            var reserve = GetReserve(isPlayerTeam);

            for (var i = 0; i < SlotCount; i++)
            {
                var model = new CardModel(deck[i], isPlayerTeam, i);
                field[i] = model;
            }

            for (var i = SlotCount; i < SlotCount * 2; i++)
            {
                reserve.Add(new CardModel(deck[i], isPlayerTeam, -1));
            }
        }
    }
}
