using System;
using System.Collections.Generic;
using System.Threading;
using CardGame.CardBattle.Core;
using CardGame.CardBattle.Input;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardGame.CardBattle.Cards
{
    public enum CardBoardPhase
    {
        Hidden,
        ReserveFaceDown,
        BattlefieldFaceUp
    }

    /// <summary>3D 카드 엔티티 — 앞/뒤 쿼드, 배치 플립, 드래그 타겟팅. 히트는 ShakeRoot 콜라이더.</summary>
    public sealed partial class CardEntity : MonoBehaviour,
        ICardBattleView,
        ICardBoardMotion,
        ICardInputHost,
        IDragSource,
        IDropTarget,
        IDragHoverVisual,
        IPointerClickHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerMoveHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private static readonly List<RaycastResult> PointerRaycastResults = new List<RaycastResult>(16);

        [SerializeField] private Transform shakeRoot;
        [SerializeField] private CardFaceView frontFace;
        [SerializeField] private CardFaceView backFace;
        [SerializeField] private TextMeshPro nameLabel;
        [SerializeField] private TextMeshPro hpLabel;
        [SerializeField] private CardHpBarView hpBar;
        private float deployMoveDuration = 0.45f;
        private float flipDuration = 0.4f;
        private float attackDashDistance = 0.55f;
        private float attackDashDuration = 0.2f;
        private float deathVisualDuration = 0.35f;
        private float hpChangeDuration = 0.35f;
        private Color hoverValidColor = new Color(0.55f, 1f, 0.6f, 1f);
        private Color hoverInvalidColor = new Color(1f, 0.45f, 0.45f, 1f);

        private CardViewState viewState;
        private int displayHp;
        private int displayMaxHp;
        private Vector3 homeLocalPosition;
        private Quaternion homeLocalRotation;
        private Color frontBaseColor = Color.white;
        private Color backBaseColor = Color.white;
        private bool useDotween = true;
        private bool suppressNextClick;
        private bool dragStarted;
        private bool canBeginDragInput;
        private bool canAcceptTargetInput;
        private CardBoardPhase phase = CardBoardPhase.Hidden;
        private CardBoardMotion boardMotion;
        private CardCombatMotion combatMotion;

        private bool IsMotionValid => this != null && shakeRoot != null;

        public CardInstanceId InstanceId => viewState.InstanceId;
        public CardBoardPhase Phase => phase;
        public Transform ViewTransform => transform;
        public Transform InputTransform => transform;
        public object DragPayload => viewState.InstanceId;
        public Transform DragTransform => transform;
        public bool CanBeginDrag => canBeginDragInput;
        public bool CanAcceptTarget => canAcceptTargetInput;
        public object DropPayload => viewState.InstanceId;
        public Transform DropTransform => transform;

        public event Action<ICardInputHost> Clicked;
        public event Action<ICardInputHost> LongPressed;
        public event Action<ICardInputHost> LongPressReleased;
        public event Action<ICardInputHost, Vector2> DragStarted;
        public event Action<ICardInputHost, ICardInputHost, Vector2> DragMoved;
        public event Action<ICardInputHost, ICardInputHost, Vector2> DragEnded;

        private void Awake()
        {
            if (shakeRoot == null)
            {
                shakeRoot = transform;
            }

            EnsureHitColliderOnShakeRoot();

            homeLocalPosition = transform.localPosition;
            homeLocalRotation = transform.localRotation;

            if (frontFace != null)
            {
                frontBaseColor = frontFace.BaseColor;
            }

            if (backFace != null)
            {
                backBaseColor = backFace.BaseColor;
            }

            try
            {
                DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            }
            catch (Exception)
            {
                useDotween = false;
            }

            boardMotion = new CardBoardMotion(this);
            combatMotion = new CardCombatMotion(this);
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

        public void ApplyBackSprite(Sprite backSprite)
        {
            if (backFace != null && backSprite != null)
            {
                backFace.ApplySprite(backSprite);
            }
        }

        public void ApplyInputTargeting(bool canBeginDrag, bool canAcceptTarget)
        {
            canBeginDragInput = canBeginDrag;
            canAcceptTargetInput = canAcceptTarget;
        }

        public void ApplyLayout(BattleLayoutConfig layoutConfig)
        {
            if (layoutConfig == null)
            {
                return;
            }

            deployMoveDuration = layoutConfig.deployMoveDuration;
            flipDuration = layoutConfig.flipDuration;
            attackDashDistance = layoutConfig.attackDashDistance;
            attackDashDuration = layoutConfig.attackDashDuration;
            hpChangeDuration = layoutConfig.hpChangeDuration;
            deathVisualDuration = layoutConfig.deathVisualDuration;
            hoverValidColor = layoutConfig.hoverValidColor;
            hoverInvalidColor = layoutConfig.hoverInvalidColor;
        }

        public void SyncFromModel(CardModel model)
        {
            Bind(model != null ? CardViewState.FromModel(model) : default);
        }

        public void Bind(CardViewState state)
        {
            viewState = state;
            displayHp = state.DisplayHp;
            displayMaxHp = state.MaxHp > 0 ? state.MaxHp : Mathf.Max(state.DisplayHp, 1);

            if (nameLabel != null)
            {
                nameLabel.text = state.IsValid ? state.DisplayName : string.Empty;
            }

            if (frontFace != null)
            {
                frontFace.ApplySprite(state.Illustration);
            }

            RefreshHpInstant();
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

        public void RefreshHpInstant()
        {
            ApplyHpVisual(displayHp);
        }

        public void SetHpDisplay(int hp)
        {
            displayHp = hp;
            ApplyHpVisual(hp);
        }

        private void ApplyHpVisual(int hp)
        {
            if (hpLabel != null)
            {
                hpLabel.text = hp.ToString();
            }

            if (hpBar != null)
            {
                hpBar.SetFill(hp, displayMaxHp);
            }
        }

        public void SetPhase(CardBoardPhase nextPhase)
        {
            phase = nextPhase;

            switch (phase)
            {
                case CardBoardPhase.Hidden:
                    gameObject.SetActive(false);
                    break;
                case CardBoardPhase.ReserveFaceDown:
                    gameObject.SetActive(true);
                    SetFaceDownInstant();
                    SetFrontLabelsVisible(false);
                    break;
                case CardBoardPhase.BattlefieldFaceUp:
                    gameObject.SetActive(true);
                    SetFaceUpInstant();
                    SetFrontLabelsVisible(true);
                    break;
            }
        }

        public Vector3 HomeLocalPosition => homeLocalPosition;

        public bool IsAtHomeLocalPosition(Vector3 localPosition)
        {
            return Vector3.Distance(homeLocalPosition, localPosition) < 0.001f;
        }

        private void SetFaceDownInstant()
        {
            if (!IsMotionValid)
            {
                return;
            }

            shakeRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private void SetFaceUpInstant()
        {
            if (!IsMotionValid)
            {
                return;
            }

            shakeRoot.localRotation = Quaternion.identity;
        }

        private void SetFrontLabelsVisible(bool visible)
        {
            if (nameLabel != null)
            {
                nameLabel.gameObject.SetActive(visible);
            }

            if (hpLabel != null)
            {
                hpLabel.gameObject.SetActive(visible);
            }

            if (hpBar != null)
            {
                hpBar.SetVisible(visible);
            }
        }
    }
}
