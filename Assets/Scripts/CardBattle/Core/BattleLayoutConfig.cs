using CardGame.CardBattle.Cards;

using Sirenix.OdinInspector;

using UnityEngine;



namespace CardGame.CardBattle.Core

{

    /// <summary>카드 프리팹·팀별 비주얼·배치 연출 파라미터. 슬롯 위치는 씬 BattleBoardZoneLayout.</summary>

    [CreateAssetMenu(fileName = "BattleLayout", menuName = "CardGame/CardBattle/Battle Layout")]

    public sealed class BattleLayoutConfig : ScriptableObject

    {

        [BoxGroup("프리팹", centerLabel: true)]

        [LabelText("카드 엔티티")]

        [Required]

        public CardEntity cardEntityPrefab;



        [BoxGroup("규칙")]

        [LabelText("전장 슬롯 수")]

        [Min(1)]

        public int battlefieldSlotCount = BattleField.SlotCount;



        [BoxGroup("규칙")]

        [LabelText("최소 덱 크기")]

        [ShowInInspector, ReadOnly]

        public int MinDeckSize => battlefieldSlotCount * 2;



        [BoxGroup("플레이어", centerLabel: true)]

        [LabelText("카드 뒷면")]

        public Sprite playerCardBack;



        [BoxGroup("적", centerLabel: true)]

        [LabelText("카드 뒷면")]

        public Sprite enemyCardBack;



        [BoxGroup("연출")]

        [LabelText("배치 이동 시간")]

        [Min(0.05f)]

        public float deployMoveDuration = 0.45f;



        [BoxGroup("연출")]

        [LabelText("뒤집기 시간")]

        [Min(0.05f)]

        public float flipDuration = 0.4f;



        public Sprite GetCardBack(bool isPlayerTeam)

        {

            return isPlayerTeam ? playerCardBack : enemyCardBack;

        }

    }

}


