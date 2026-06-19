using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>공격 1회에 대한 HP 표시용 스냅샷. 도메인 선적용 후 before/after를 보관.</summary>
    public sealed class AttackPresentationSnapshot
    {
        private readonly Dictionary<int, (int before, int after)> hpById = new Dictionary<int, (int, int)>();

        public static AttackPresentationSnapshot From(AttackOutcome outcome, BattleActionRequest request)
        {
            var snapshot = new AttackPresentationSnapshot();
            snapshot.Record(
                request.Attacker.InstanceId,
                outcome.BeforeAttackerHp,
                request.Attacker.CurrentHp);
            snapshot.Record(
                request.Target.InstanceId,
                outcome.BeforeTargetHp,
                request.Target.CurrentHp);

            var secondary = outcome.Resolution.Secondary;
            if (secondary.HasTarget)
            {
                snapshot.Record(
                    secondary.Target.InstanceId,
                    outcome.BeforeSecondaryHp,
                    secondary.Target.CurrentHp);
            }

            return snapshot;
        }

        public void Record(CardInstanceId id, int beforeHp, int afterHp)
        {
            if (!id.IsValid)
            {
                return;
            }

            hpById[id.Value] = (beforeHp, afterHp);
        }

        public bool TryGetHp(CardInstanceId id, out int beforeHp, out int afterHp)
        {
            if (id.IsValid && hpById.TryGetValue(id.Value, out var pair))
            {
                beforeHp = pair.before;
                afterHp = pair.after;
                return true;
            }

            beforeHp = 0;
            afterHp = 0;
            return false;
        }

        public int GetBeforeHp(CardInstanceId id)
        {
            return TryGetHp(id, out var before, out _) ? before : 0;
        }

        public int GetAfterHp(CardInstanceId id)
        {
            return TryGetHp(id, out _, out var after) ? after : 0;
        }
    }
}
