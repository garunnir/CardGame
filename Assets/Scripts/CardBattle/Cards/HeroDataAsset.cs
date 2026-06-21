using Sirenix.OdinInspector;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    [CreateAssetMenu(fileName = "HeroData", menuName = "CardGame/CardBattle/Hero Data")]
    public sealed class HeroDataAsset : ScriptableObject
    {
        [BoxGroup("기본", centerLabel: true)]
        public string heroId;

        [BoxGroup("기본")]
        public string displayName = "Hero";

        [BoxGroup("기본")]
        [Min(1)]
        public int maxHp = 20;

        [BoxGroup("기본")]
        [Min(1)]
        public int baseAttack = 4;

        [BoxGroup("기본")]
        [Min(1)]
        public int maxMp = 100;

        [BoxGroup("기본")]
        [LabelText("턴 시작 MP")]
        [Min(0)]
        public int mpGainPerTurn = 12;

        [BoxGroup("행동", centerLabel: true)]
        [Required]
        public HeroNormalAttackBehaviorAsset normalAttackBehavior;

        [BoxGroup("행동")]
        [Required]
        public HeroShieldBehaviorAsset shieldBehavior;

        [BoxGroup("기본")]
        [PreviewField(Height = 80)]
        public Sprite portrait;
    }
}
