using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>씬 앵커 — 전장은 중심+간격으로 런타임 정렬, 대기열은 스택 원점.</summary>
    public sealed class BattleBoardZoneLayout : MonoBehaviour
    {
        [BoxGroup("팀")]
        [LabelText("플레이어 진영")]
        [SerializeField] private bool isPlayerTeam = true;

        [BoxGroup("전장", centerLabel: true)]
        [LabelText("중심 앵커")]
        [Required]
        [ValidateInput(nameof(HasBattlefieldCenter), "전장 중심 앵커가 필요합니다.")]
        [SerializeField] private Transform battlefieldCenter;

        [BoxGroup("전장")]
        [LabelText("카드 간격")]
        [Min(0f)]
        [SerializeField] private float cardSpacing = 2f;

        [BoxGroup("대기열", centerLabel: true)]
        [LabelText("스택 원점")]
        [Required]
        [SerializeField] private Transform reserveStackOrigin;

        [BoxGroup("대기열")]
        [LabelText("스택 간격 (StackOrigin 로컬)")]
        [SerializeField] private Vector3 reserveStackOffset;

        [BoxGroup("영웅", centerLabel: true)]
        [LabelText("측면 앵커")]
        [SerializeField] private Transform heroAnchor;

        public bool IsPlayerTeam => isPlayerTeam;

        public Transform BattlefieldCenter => battlefieldCenter;

        public Transform HeroAnchor => heroAnchor;

        public float CardSpacing => cardSpacing;

        public Transform ReserveStackOrigin => reserveStackOrigin;

        /// <summary>StackOrigin 로컬 오프셋 — 앵커 회전을 따름.</summary>
        public Vector3 GetReserveStackLocalOffset(int stackIndex)
        {
            return reserveStackOffset * Mathf.Max(0, stackIndex);
        }

        public bool TryComputeBattlefieldPose(
            CardModel model,
            CardModel[] battlefield,
            out Vector3 localPosition,
            out Quaternion localRotation)
        {
            localPosition = Vector3.zero;
            localRotation = Quaternion.identity;

            if (battlefieldCenter == null || model == null || battlefield == null)
            {
                return false;
            }

            var displayIndex = -1;
            var aliveCount = 0;
            for (var i = 0; i < battlefield.Length; i++)
            {
                var card = battlefield[i];
                if (card == null || !card.IsAlive)
                {
                    continue;
                }

                if (card == model)
                {
                    displayIndex = aliveCount;
                }

                aliveCount++;
            }

            if (displayIndex < 0)
            {
                return false;
            }

            var offset = (displayIndex - (aliveCount - 1) * 0.5f) * cardSpacing;
            localPosition = Vector3.right * offset;
            return true;
        }

        public void Configure(
            bool playerTeam,
            Transform center,
            Transform reserveOrigin,
            Transform hero = null,
            float? spacing = null)
        {
            isPlayerTeam = playerTeam;
            battlefieldCenter = center;
            reserveStackOrigin = reserveOrigin;
            if (spacing.HasValue)
            {
                cardSpacing = spacing.Value;
            }

            if (hero != null)
            {
                heroAnchor = hero;
            }
        }

        private bool HasBattlefieldCenter => battlefieldCenter != null;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (battlefieldCenter != null)
            {
                Gizmos.color = new Color(0.2f, 0.85f, 0.35f, 0.85f);
                var maxCards = BattleField.SlotCount;
                for (var i = 0; i < maxCards; i++)
                {
                    var offset = (i - (maxCards - 1) * 0.5f) * cardSpacing;
                    var localPos = Vector3.right * offset;
                    BattleBoardGizmos.DrawFlatCardRect(
                        battlefieldCenter,
                        battlefieldCenter.TransformPoint(localPos));
                }
            }

            if (reserveStackOrigin == null)
            {
                return;
            }

            Gizmos.color = new Color(0.35f, 0.55f, 0.95f, 0.85f);
            BattleBoardGizmos.DrawFlatCardRect(
                reserveStackOrigin,
                reserveStackOrigin.TransformPoint(GetReserveStackLocalOffset(0)));

            Gizmos.color = new Color(0.35f, 0.55f, 0.95f, 0.35f);
            for (var i = 1; i < 3; i++)
            {
                BattleBoardGizmos.DrawFlatCardRect(
                    reserveStackOrigin,
                    reserveStackOrigin.TransformPoint(GetReserveStackLocalOffset(i)));
            }

            if (heroAnchor != null)
            {
                Gizmos.color = new Color(0.95f, 0.75f, 0.25f, 0.9f);
                BattleBoardGizmos.DrawFlatCardRect(heroAnchor, heroAnchor.position);
            }
        }
#endif
    }
}
