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

        private readonly HashSet<CardModel> pendingBattlefieldDeploy = new HashSet<CardModel>();

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
            pendingBattlefieldDeploy.Clear();
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

        /// <summary>해당 카드가 팀 전장 슬롯에 배치되어 있는지. 앞면 공개·타겟 가능 여부의 도메인 SSOT.</summary>
        public bool IsOnBattlefield(CardModel model)
        {
            if (model == null || !model.IsAlive)
            {
                return false;
            }

            var field = GetBattlefield(model.IsPlayerTeam);
            for (var i = 0; i < SlotCount; i++)
            {
                if (field[i] == model)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>전장 슬롯에 있으나 배치 연출 중 — 타겟·공격 불가.</summary>
        public void MarkPendingBattlefieldDeploy(CardModel model)
        {
            if (model != null)
            {
                pendingBattlefieldDeploy.Add(model);
            }
        }

        public void ClearPendingBattlefieldDeploy(CardModel model)
        {
            if (model != null)
            {
                pendingBattlefieldDeploy.Remove(model);
            }
        }

        public bool IsPendingBattlefieldDeploy(CardModel model)
        {
            return model != null && pendingBattlefieldDeploy.Contains(model);
        }

        /// <summary>전장 배치 완료(연출 포함) 후 타겟·공격 가능.</summary>
        public bool IsTargetableOnBattlefield(CardModel model)
        {
            return IsOnBattlefield(model) && !IsPendingBattlefieldDeploy(model);
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
