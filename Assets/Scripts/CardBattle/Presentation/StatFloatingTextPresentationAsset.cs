using Sirenix.OdinInspector;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    [CreateAssetMenu(
        fileName = "StatFloatingTextPresentation",
        menuName = "CardGame/CardBattle/Presentation/Stat Floating Text")]
    public sealed class StatFloatingTextPresentationAsset : ScriptableObject
    {
        private const string DefaultResourcePath = "CardBattle/Presentation/StatFloatingText_Default";

        [FoldoutGroup("위치", expanded: true)]
        [LabelText("기준 Transform")]
        public StatFloatingTextOrientationMode orientationMode = StatFloatingTextOrientationMode.FollowHpLabel;

        [FoldoutGroup("위치")]
        [LabelText("스폰 오프셋 (기준 로컬)")]
        [Tooltip("기준 Transform 기준 로컬 좌표. HpLabel 모드면 라벨 위치에서, TargetRoot면 카드 중심에서 적용")]
        public Vector3 spawnOffsetLocal = new Vector3(0f, 0.25f, 0.02f);

        [FoldoutGroup("위치")]
        [LabelText("면 전방 오프셋")]
        [Tooltip("카드/영웅 면 법선 방향으로 추가 밀어냄. 초상·카드 면에 가려질 때 증가")]
        [Min(0f)]
        public float faceForwardOffset = 0.02f;

        [FoldoutGroup("회전")]
        [LabelText("회전 보정 (오일러)")]
        [Tooltip("Follow 모드: 기준 회전 위에 추가. WorldEuler 모드: 월드 회전으로 사용. 뒤집히면 Y=180 등 조정")]
        public Vector3 rotationEuler = Vector3.zero;

        [FoldoutGroup("이동")]
        [LabelText("상승 방향 (기준 로컬)")]
        public Vector3 riseDirectionLocal = Vector3.up;

        [FoldoutGroup("이동")]
        [LabelText("상승 거리")]
        [Min(0f)]
        public float riseDistance = 0.55f;

        [FoldoutGroup("이동")]
        [LabelText("재생 시간")]
        [Min(0.05f)]
        public float duration = 0.75f;

        [FoldoutGroup("텍스트")]
        [LabelText("글자 크기")]
        [Min(0.1f)]
        public float fontSize = 4f;

        [FoldoutGroup("텍스트")]
        [LabelText("Sorting Order")]
        [Tooltip("0이면 카드/영웅 기본값 사용. 지정 시 그 값과 기본값 중 큰 쪽 적용")]
        public int sortingOrder;

        [FoldoutGroup("텍스트")]
        [LabelText("카드 면 위에 렌더")]
        [Tooltip("TMP Overlay 셰이더 사용 — 초상 depth에 가려지지 않음")]
        public bool renderAboveCardFace = true;

        [FoldoutGroup("색상")]
        public Color healColor = new Color(0.45f, 1f, 0.55f, 1f);

        [FoldoutGroup("색상")]
        public Color damageColor = new Color(1f, 0.35f, 0.35f, 1f);

        [FoldoutGroup("페이드")]
        [LabelText("페이드 시작 (재생 비율)")]
        [Range(0f, 1f)]
        public float fadeStartRatio = 0.65f;

        [FoldoutGroup("페이드")]
        [LabelText("페이드 길이 (재생 비율)")]
        [Range(0.05f, 1f)]
        public float fadeDurationRatio = 0.35f;

        public static StatFloatingTextPresentationAsset LoadDefault()
        {
            return Resources.Load<StatFloatingTextPresentationAsset>(DefaultResourcePath);
        }
    }
}
