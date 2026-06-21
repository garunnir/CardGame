using Sirenix.OdinInspector;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    [CreateAssetMenu(
        fileName = "ProjectilePresentation",
        menuName = "CardGame/CardBattle/Presentation/Projectile Presentation")]
    public sealed class ProjectilePresentationAsset : ScriptableObject
    {
        [FoldoutGroup("비행", expanded: true)]
        [LabelText("투사체 VFX")]
        public GameObject projectileVfxPrefab;

        [FoldoutGroup("비행")]
        [LabelText("발사 SFX")]
        public AudioClip launchSfx;

        [FoldoutGroup("비행")]
        [LabelText("비행 시간")]
        [Min(0f)]
        public float flightDuration = 0.35f;

        [FoldoutGroup("비행")]
        [LabelText("경로")]
        public ProjectilePathKind pathKind = ProjectilePathKind.Linear;

        [FoldoutGroup("비행")]
        [LabelText("포물선 높이")]
        [Tooltip("Arc 전용. 0이면 0.5f")]
        [Min(0f)]
        public float arcHeight;

        [FoldoutGroup("도착")]
        [LabelText("Impact SFX")]
        public AudioClip impactSfx;

        [FoldoutGroup("도착")]
        [LabelText("Impact VFX")]
        public GameObject impactVfxPrefab;
    }
}
