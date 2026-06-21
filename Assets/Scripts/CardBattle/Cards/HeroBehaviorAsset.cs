using Sirenix.OdinInspector;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    public abstract class HeroBehaviorAsset : ScriptableObject
    {
        [BoxGroup("식별", centerLabel: true)]
        [LabelText("행동 ID")]
        public string behaviorId;

        [BoxGroup("상세 보기", centerLabel: true)]
        [LabelText("표시 이름")]
        public string displayName;

        [BoxGroup("상세 보기")]
        [TextArea(2, 4)]
        public string description;
    }
}
