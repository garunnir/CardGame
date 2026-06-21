using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    [Serializable]
    public class HeroNormalAttackPresentation
    {
        [FoldoutGroup("사운드", expanded: true)]
        [LabelText("타격")]
        public AudioClip strikeSfx;

        [FoldoutGroup("사운드")]
        [LabelText("피격")]
        public AudioClip receivedHitSfx;

        [FoldoutGroup("VFX")]
        [LabelText("타격 이펙트")]
        public GameObject strikeVfxPrefab;

        [FoldoutGroup("VFX")]
        [LabelText("피격 이펙트")]
        public GameObject receivedHitVfxPrefab;

        [FoldoutGroup("모션")]
        [LabelText("돌진 시간")]
        [Min(0f)]
        public float strikeDashDuration = 0.25f;

        [FoldoutGroup("모션")]
        [LabelText("피격 셰이크")]
        [Min(0f)]
        public float hitShakeStrength = 0.12f;

        [FoldoutGroup("모션")]
        [LabelText("카메라 셰이크")]
        [Min(0f)]
        public float cameraShake = 0.12f;
    }

    [Serializable]
    public class HeroShieldPresentation
    {
        [FoldoutGroup("사운드", expanded: true)]
        [LabelText("보호막 버프")]
        public AudioClip shieldBuffSfx;

        [FoldoutGroup("VFX")]
        [LabelText("보호막 버프")]
        public GameObject shieldBuffVfxPrefab;

        [FoldoutGroup("모션")]
        [LabelText("버프 Bloom")]
        [Min(0f)]
        public float buffBloomIntensity = 0.15f;
    }
}
