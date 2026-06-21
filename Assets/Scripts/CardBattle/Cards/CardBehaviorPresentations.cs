using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    [Serializable]
    public class NormalBehaviorPresentation
    {
        [FoldoutGroup("사운드", expanded: true)]
        [LabelText("공격")]
        public AudioClip attackSfx;

        [FoldoutGroup("사운드")]
        [LabelText("명중")]
        public AudioClip hitSfx;

        [FoldoutGroup("사운드")]
        [LabelText("피격")]
        public AudioClip receivedHitSfx;

        [FoldoutGroup("사운드")]
        [LabelText("반격 (미사용)")]
        [Tooltip("반격 연출은 방어자 명중 슬롯을 사용합니다.")]
        public AudioClip counterSfx;

        [FoldoutGroup("사운드")]
        [LabelText("사망")]
        public AudioClip deathSfx;

        [FoldoutGroup("VFX")]
        [LabelText("공격 이펙트")]
        public GameObject attackVfxPrefab;

        [FoldoutGroup("VFX")]
        [LabelText("명중 이펙트")]
        public GameObject hitVfxPrefab;

        [FoldoutGroup("VFX")]
        [LabelText("피격 이펙트")]
        public GameObject receivedHitVfxPrefab;

        [FoldoutGroup("VFX")]
        [LabelText("사망 이펙트")]
        public GameObject deathVfxPrefab;

        [FoldoutGroup("모션")]
        [LabelText("돌진 시간")]
        [Tooltip("0이면 CardEntity 기본값")]
        [Min(0f)]
        public float attackDashDuration;

        [FoldoutGroup("모션")]
        [LabelText("피격 셰이크 강도")]
        [Tooltip("0이면 CardEntity 기본값")]
        [Min(0f)]
        public float hitShakeStrength;
    }

    [Serializable]
    public class RangedBehaviorPresentation
    {
        [FoldoutGroup("사운드", expanded: true)]
        [LabelText("피격")]
        public AudioClip receivedHitSfx;

        [FoldoutGroup("사운드")]
        [LabelText("사망")]
        public AudioClip deathSfx;

        [FoldoutGroup("VFX")]
        [LabelText("피격 이펙트")]
        public GameObject receivedHitVfxPrefab;

        [FoldoutGroup("VFX")]
        [LabelText("사망 이펙트")]
        public GameObject deathVfxPrefab;
    }

    [Serializable]
    public class MusouBehaviorPresentation
    {
        [FoldoutGroup("사운드", expanded: true)]
        [LabelText("1타 공격")]
        public AudioClip attackSfx;

        [FoldoutGroup("사운드")]
        [LabelText("1타 명중")]
        public AudioClip hitSfx;

        [FoldoutGroup("사운드")]
        [LabelText("2타 명중")]
        public AudioClip secondaryHitSfx;

        [FoldoutGroup("사운드")]
        [LabelText("피격")]
        public AudioClip receivedHitSfx;

        [FoldoutGroup("사운드")]
        [LabelText("반격 (미사용)")]
        [Tooltip("반격 연출은 방어자 명중 슬롯을 사용합니다.")]
        public AudioClip counterSfx;

        [FoldoutGroup("사운드")]
        [LabelText("사망")]
        public AudioClip deathSfx;

        [FoldoutGroup("VFX")]
        [LabelText("1타 공격")]
        public GameObject attackVfxPrefab;

        [FoldoutGroup("VFX")]
        [LabelText("1타 명중")]
        public GameObject hitVfxPrefab;

        [FoldoutGroup("VFX")]
        [LabelText("2타 명중")]
        public GameObject secondaryHitVfxPrefab;

        [FoldoutGroup("VFX")]
        [LabelText("피격 이펙트")]
        public GameObject receivedHitVfxPrefab;

        [FoldoutGroup("VFX")]
        [LabelText("사망 이펙트")]
        public GameObject deathVfxPrefab;

        [FoldoutGroup("모션")]
        [LabelText("돌진 시간")]
        [Min(0f)]
        public float attackDashDuration;

        [FoldoutGroup("모션")]
        [LabelText("2타 대기")]
        [Min(0f)]
        public float secondaryHitDelay = 0.15f;

        [FoldoutGroup("모션")]
        [LabelText("2타 카메라 셰이크")]
        [Min(0f)]
        public float secondaryCameraShake = 0.2f;
    }

    [Serializable]
    public class HealerBehaviorPresentation
    {
        [FoldoutGroup("사운드", expanded: true)]
        [LabelText("공격")]
        public AudioClip attackSfx;

        [FoldoutGroup("사운드")]
        [LabelText("명중")]
        public AudioClip hitSfx;

        [FoldoutGroup("사운드")]
        [LabelText("피격")]
        public AudioClip receivedHitSfx;

        [FoldoutGroup("사운드")]
        [LabelText("반격 (미사용)")]
        [Tooltip("반격 연출은 방어자 명중 슬롯을 사용합니다.")]
        public AudioClip counterSfx;

        [FoldoutGroup("사운드")]
        [LabelText("사망")]
        public AudioClip deathSfx;

        [FoldoutGroup("VFX")]
        [LabelText("공격")]
        public GameObject attackVfxPrefab;

        [FoldoutGroup("VFX")]
        [LabelText("명중")]
        public GameObject hitVfxPrefab;

        [FoldoutGroup("VFX")]
        [LabelText("피격")]
        public GameObject receivedHitVfxPrefab;

        [FoldoutGroup("VFX")]
        [LabelText("사망")]
        public GameObject deathVfxPrefab;

        [FoldoutGroup("모션")]
        [LabelText("힐 Bloom 강도")]
        [Min(0f)]
        public float healBloomIntensity;
    }
}
