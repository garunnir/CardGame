using System;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardGame.CardBattle.Cards
{
    /// <summary>3D 영웅 카드 — 초상·HP/보호막/MP·전투 연출·입력.</summary>
    public sealed partial class HeroEntity : MonoBehaviour,
        IHeroBattleView,
        IHeroInputHost,
        IPointerClickHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerMoveHandler,
        IPointerExitHandler
    {
        [SerializeField] private Transform shakeRoot;
        [SerializeField] private CardFaceView frontFace;
        [SerializeField] private TextMeshPro nameLabel;
        [SerializeField] private TextMeshPro hpLabel;
        [SerializeField] private TextMeshPro mpLabel;
        [SerializeField] private HeroHpShieldBarView hpShieldBar;
        [SerializeField] private CardHpBarView mpBar;

        private float attackDashDistance = 0.55f;
        private float attackDashDuration = 0.2f;
        private float hpChangeDuration = 0.35f;
        private Color frontBaseColor = Color.white;
        private Color targetHighlightColor = new Color(0.55f, 1f, 0.6f, 1f);

        private HeroViewState viewState;
        private int displayHp;
        private int displayShield;
        private int displayMp;
        private int displayMaxHp;
        private int displayMaxMp;
        private bool useDotween = true;
        private bool targetEnabled;
        private HeroCombatMotion combatMotion;

        private bool IsMotionValid => this != null && shakeRoot != null;

        public HeroInstanceId InstanceId => viewState.InstanceId;
        public Transform ViewTransform => transform;
        public bool IsEnemyHero => viewState.IsValid && !viewState.IsPlayerTeam;
        public bool CanAcceptShortClick => targetEnabled && IsEnemyHero;

        public event Action<IHeroInputHost> ShortClicked;
        public event Action<IHeroInputHost> LongPressed;
        public event Action<IHeroInputHost> LongPressReleased;

        private void Awake()
        {
            if (shakeRoot == null)
            {
                shakeRoot = transform;
            }

            EnsureHitColliderOnShakeRoot();

            if (frontFace != null)
            {
                frontBaseColor = frontFace.BaseColor;
            }

            try
            {
                DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            }
            catch (Exception)
            {
                useDotween = false;
            }

            combatMotion = new HeroCombatMotion(this);
            ApplyVisualLayering();
        }

        private void ApplyVisualLayering()
        {
            if (frontFace != null)
            {
                var portraitTransform = frontFace.transform;
                var portraitPos = portraitTransform.localPosition;
                portraitPos.z = HeroVisualSorting.PortraitLocalZ;
                portraitTransform.localPosition = portraitPos;

                var portraitRenderer = frontFace.GetComponent<MeshRenderer>();
                if (portraitRenderer != null)
                {
                    portraitRenderer.sortingOrder = HeroVisualSorting.Portrait;
                }
            }

            if (hpShieldBar != null)
            {
                var barPos = hpShieldBar.transform.localPosition;
                barPos.z = HeroVisualSorting.BarLocalZ;
                hpShieldBar.transform.localPosition = barPos;
                hpShieldBar.ApplySortingOrders();
            }

            if (mpBar != null)
            {
                var mpPos = mpBar.transform.localPosition;
                mpPos.z = HeroVisualSorting.BarLocalZ;
                mpBar.transform.localPosition = mpPos;
                mpBar.ApplySortingOrders(HeroVisualSorting.BarBackground, HeroVisualSorting.BarFill);
            }

            if (nameLabel != null)
            {
                nameLabel.sortingOrder = HeroVisualSorting.NameLabel;
            }

            if (hpLabel != null)
            {
                hpLabel.sortingOrder = HeroVisualSorting.StatLabel;
            }

            if (mpLabel != null)
            {
                mpLabel.sortingOrder = HeroVisualSorting.StatLabel;
            }
        }

        private void OnDestroy()
        {
            CancelLongPressOnDestroy();
            combatMotion?.Dispose();
            transform.DOKill();
            if (shakeRoot != null && shakeRoot != transform)
            {
                shakeRoot.DOKill();
            }
        }

        public void ApplyLayout(BattleLayoutConfig layoutConfig)
        {
            if (layoutConfig == null)
            {
                return;
            }

            attackDashDistance = layoutConfig.attackDashDistance;
            attackDashDuration = layoutConfig.attackDashDuration;
            hpChangeDuration = layoutConfig.hpChangeDuration;
            targetHighlightColor = layoutConfig.hoverValidColor;
        }

        public void SyncFromModel(HeroModel model)
        {
            Bind(model != null ? HeroViewState.FromModel(model) : default);
        }

        public void Bind(HeroViewState state)
        {
            if (!this)
            {
                return;
            }

            viewState = state;
            displayHp = state.DisplayHp;
            displayShield = state.DisplayShield;
            displayMp = state.DisplayMp;
            displayMaxHp = state.MaxHp > 0 ? state.MaxHp : Mathf.Max(state.DisplayHp, 1);
            displayMaxMp = state.MaxMp > 0 ? state.MaxMp : Mathf.Max(state.DisplayMp, 1);

            if (nameLabel != null)
            {
                nameLabel.text = state.IsValid ? state.DisplayName : string.Empty;
            }

            if (frontFace != null)
            {
                frontFace.ApplySprite(state.Portrait);
            }

            RefreshStatsInstant();
            gameObject.SetActive(state.IsValid);
        }

        public void RefreshStatsInstant()
        {
            SetStatsVisual(displayHp, displayShield, displayMp);
        }

        public void SetStats(int hp, int shield, int mp)
        {
            displayHp = hp;
            displayShield = shield;
            displayMp = mp;
            SetStatsVisual(hp, shield, mp);
        }

        public void SetHpDisplay(int hp)
        {
            displayHp = hp;
            SetStatsVisual(hp, displayShield, displayMp);
        }

        internal int DisplayMaxHpValue => displayMaxHp;

        internal int DisplayMaxMpValue => displayMaxMp;

        internal int DisplayShieldValue => displayShield;

        internal int DisplayHpValue => displayHp;

        public void SetTargetHighlight(bool enabled)
        {
            targetEnabled = enabled;
            if (frontFace == null)
            {
                return;
            }

            frontFace.SetColor(enabled && IsEnemyHero ? targetHighlightColor : frontBaseColor);
        }

        private void EnsureHitColliderOnShakeRoot()
        {
            var legacyRootCollider = GetComponent<BoxCollider>();
            if (legacyRootCollider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(legacyRootCollider);
                }
                else
                {
                    DestroyImmediate(legacyRootCollider);
                }
            }

            var hit = shakeRoot.GetComponent<BoxCollider>();
            if (hit == null)
            {
                hit = shakeRoot.gameObject.AddComponent<BoxCollider>();
            }

            hit.center = Vector3.zero;
            hit.size = new Vector3(CardFaceView.DefaultWidth, CardFaceView.DefaultHeight, 0.08f);
        }

        internal void SetStatsVisual(int hp, int shield, int mp)
        {
            if (hpShieldBar != null)
            {
                hpShieldBar.SetFill(hp, shield, displayMaxHp);
            }

            if (mpBar != null)
            {
                mpBar.SetFill(mp, displayMaxMp);
            }

            if (hpLabel != null)
            {
                hpLabel.text = shield > 0
                    ? $"HP {hp}/{displayMaxHp} (+{shield})"
                    : $"HP {hp}/{displayMaxHp}";
            }

            if (mpLabel != null)
            {
                mpLabel.text = $"MP {mp}/{displayMaxMp}";
            }
        }
    }
}
