using CardGame.CardBattle.Cards;

using CardGame.CardBattle.Presentation;

using Sirenix.OdinInspector;

using UnityEngine;



namespace CardGame.CardBattle.Core

{

    /// <summary>카드 프리팹·팀별 비주얼·배치 연출 파라미터. 전장 위치는 씬 BattleBoardZoneLayout 중심+간격.</summary>

    [CreateAssetMenu(fileName = "BattleLayout", menuName = "CardGame/CardBattle/Battle Layout")]

    public sealed class BattleLayoutConfig : ScriptableObject

    {

        [BoxGroup("프리팹", centerLabel: true)]

        [LabelText("카드 엔티티")]

        [Required]

        public CardEntity cardEntityPrefab;



        [BoxGroup("프리팹", centerLabel: true)]

        [LabelText("영웅 엔티티")]

        [Required]

        public HeroEntity heroEntityPrefab;



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



        [BoxGroup("연출")]

        [LabelText("공격 연출 tail")]

        [Tooltip("공격 시퀀스 마지막 대기 시간")]

        [Min(0f)]

        public float attackPresentationTailDelay = 0.55f;



        [BoxGroup("연출")]

        [LabelText("공격 돌진 거리")]

        [Min(0f)]

        public float attackDashDistance = 0.55f;



        [BoxGroup("연출")]

        [LabelText("공격 돌진 시간")]

        [Tooltip("행동 SO가 0이면 이 값 사용")]

        [Min(0f)]

        public float attackDashDuration = 0.2f;



        [BoxGroup("연출")]

        [LabelText("HP 변화 연출 시간")]

        [Min(0.05f)]

        public float hpChangeDuration = 0.35f;



        [BoxGroup("연출")]

        [LabelText("피해/힐 플로팅 텍스트")]

        public StatFloatingTextPresentationAsset statFloatingTextPresentation;



        [BoxGroup("연출")]

        [LabelText("사망 연출 시간")]

        [Min(0.05f)]

        public float deathVisualDuration = 0.35f;



        [BoxGroup("입력")]

        [LabelText("호버 — 유효")]

        public Color hoverValidColor = new Color(0.55f, 1f, 0.6f, 1f);



        [BoxGroup("입력")]

        [LabelText("호버 — 무효")]

        public Color hoverInvalidColor = new Color(1f, 0.45f, 0.45f, 1f);



        public Sprite GetCardBack(bool isPlayerTeam)

        {

            return isPlayerTeam ? playerCardBack : enemyCardBack;

        }

    }

}

