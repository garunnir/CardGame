using CardGame.CardBattle.Cards;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>씬 앵커 — 위치·회전은 Inspector에서 직접 지정. 런타임은 identity 로컬만 적용.</summary>
    public sealed class BattleBoardZoneLayout : MonoBehaviour
    {
        [BoxGroup("팀")]
        [LabelText("플레이어 진영")]
        [SerializeField] private bool isPlayerTeam = true;

        [BoxGroup("전장", centerLabel: true)]
        [LabelText("슬롯 앵커")]
        [Required]
        [ValidateInput(nameof(HasBattlefieldSlots), "전장 슬롯 앵커가 필요합니다.")]
        [SerializeField] private Transform[] battlefieldSlots;

        [BoxGroup("대기열", centerLabel: true)]
        [LabelText("스택 원점")]
        [Required]
        [SerializeField] private Transform reserveStackOrigin;

        [BoxGroup("대기열")]
        [LabelText("스택 간격 (StackOrigin 로컬)")]
        [SerializeField] private Vector3 reserveStackOffset;

        public int SlotCount => battlefieldSlots != null ? battlefieldSlots.Length : 0;

        public bool IsPlayerTeam => isPlayerTeam;

        public Transform ReserveStackOrigin => reserveStackOrigin;

        public Transform GetBattlefieldSlotAnchor(int slotIndex)
        {
            if (battlefieldSlots == null || battlefieldSlots.Length == 0)
            {
                return null;
            }

            var index = Mathf.Clamp(slotIndex, 0, battlefieldSlots.Length - 1);
            return battlefieldSlots[index];
        }

        /// <summary>StackOrigin 로컬 오프셋 — 앵커 회전을 따름.</summary>
        public Vector3 GetReserveStackLocalOffset(int stackIndex)
        {
            return reserveStackOffset * Mathf.Max(0, stackIndex);
        }

        public void Configure(
            bool playerTeam,
            Transform[] slots,
            Transform reserveOrigin)
        {
            isPlayerTeam = playerTeam;
            battlefieldSlots = slots;
            reserveStackOrigin = reserveOrigin;
        }

        private bool HasBattlefieldSlots => battlefieldSlots != null && battlefieldSlots.Length > 0;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (battlefieldSlots != null)
            {
                Gizmos.color = new Color(0.2f, 0.85f, 0.35f, 0.85f);
                for (var i = 0; i < battlefieldSlots.Length; i++)
                {
                    var anchor = battlefieldSlots[i];
                    if (anchor == null)
                    {
                        continue;
                    }

                    BattleBoardGizmos.DrawFlatCardRect(anchor);
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
        }
#endif
    }
}
