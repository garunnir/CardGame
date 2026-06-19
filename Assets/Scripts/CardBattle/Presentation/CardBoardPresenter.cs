using System;
using System.Collections.Generic;
using System.Threading;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>3D 카드 보드 스폰·대기열/전장 배치·플립 연출.</summary>
    public sealed class CardBoardPresenter : MonoBehaviour, ICardViewRegistry, ICardBoardSession
    {
        private sealed class BoardEntitySlot
        {
            public CardModel Model;
            public CardEntity Entity;
        }

        [SerializeField] private BattleLayoutConfig layout;
        [SerializeField] private Transform playerBoardRoot;
        [SerializeField] private Transform enemyBoardRoot;

        private readonly Dictionary<CardInstanceId, BoardEntitySlot> boardSlots =
            new Dictionary<CardInstanceId, BoardEntitySlot>();
        private readonly List<ICardInputHost> inputHosts = new List<ICardInputHost>();
        private readonly List<CardModel> modelBuffer = new List<CardModel>(16);
        private readonly HashSet<CardModel> modelSetBuffer = new HashSet<CardModel>();
        private bool isBuilt;
        private int boardGeneration;
        private readonly SemaphoreSlim presentationLock = new SemaphoreSlim(1, 1);

        public event Action InputHostsChanged;

        public IReadOnlyList<ICardInputHost> InputHosts => inputHosts;

        public BattleLayoutConfig Layout => layout;

        public void Configure(
            BattleLayoutConfig battleLayout,
            Transform playerRoot,
            Transform enemyRoot)
        {
            layout = battleLayout;
            playerBoardRoot = playerRoot;
            enemyBoardRoot = enemyRoot;
        }

        public void ClearBoard()
        {
            boardGeneration++;

            foreach (var pair in boardSlots)
            {
                if (pair.Value.Entity != null)
                {
                    pair.Value.Entity.CancelMotion();
                    Destroy(pair.Value.Entity.gameObject);
                }
            }

            boardSlots.Clear();
            inputHosts.Clear();
            isBuilt = false;
        }

        public UniTask BuildBoardAsync(BattleField field)
        {
            if (layout == null || layout.cardEntityPrefab == null)
            {
                Debug.LogError("[CardBattle] BattleLayoutConfig 또는 CardEntity 프리팹이 없습니다.");
                return UniTask.CompletedTask;
            }

            if (!HasZoneLayouts())
            {
                return UniTask.CompletedTask;
            }

            return EnqueuePresentation(() => BuildBoardInternalAsync(field));
        }

        public UniTask SyncBoardAsync(BattleField field, bool animateRefill)
        {
            if (layout == null || layout.cardEntityPrefab == null)
            {
                return UniTask.CompletedTask;
            }

            if (!HasZoneLayouts())
            {
                return UniTask.CompletedTask;
            }

            return EnqueuePresentation(() => SyncBoardInternalAsync(field, animateRefill));
        }

        /// <summary>이미 presentationLock을 보유한 컨텍스트(전투 연출 등)에서만 호출.</summary>
        public UniTask SyncBoardWithinLockAsync(BattleField field, bool animateRefill)
        {
            if (layout == null || layout.cardEntityPrefab == null || !HasZoneLayouts())
            {
                return UniTask.CompletedTask;
            }

            return SyncBoardInternalAsync(field, animateRefill);
        }

        public UniTask RunExclusiveAsync(Func<UniTask> work)
        {
            return EnqueuePresentation(work);
        }

        private async UniTask EnqueuePresentation(Func<UniTask> work)
        {
            await presentationLock.WaitAsync();
            try
            {
                await work();
            }
            finally
            {
                presentationLock.Release();
            }
        }

        private void OnDestroy()
        {
            presentationLock.Dispose();
        }

        private async UniTask BuildBoardInternalAsync(BattleField field)
        {
            ClearBoard();
            var generation = boardGeneration;

            await UniTask.WhenAll(
                SpawnTeam(field, field.PlayerBattlefield, field.PlayerReserve, playerBoardRoot, true, generation),
                SpawnTeam(field, field.EnemyBattlefield, field.EnemyReserve, enemyBoardRoot, false, generation));

            if (!IsCurrentGeneration(generation))
            {
                return;
            }

            RebuildInputHosts(field);
            RefreshAllInputTargeting(field);
            isBuilt = true;
            InputHostsChanged?.Invoke();
        }

        private async UniTask SyncBoardInternalAsync(BattleField field, bool animateRefill)
        {
            if (!isBuilt)
            {
                await BuildBoardInternalAsync(field);
                return;
            }

            var generation = boardGeneration;
            await UniTask.WhenAll(
                SyncTeam(field, field.PlayerBattlefield, field.PlayerReserve, playerBoardRoot, true, animateRefill, generation),
                SyncTeam(field, field.EnemyBattlefield, field.EnemyReserve, enemyBoardRoot, false, animateRefill, generation));

            if (!IsCurrentGeneration(generation))
            {
                return;
            }

            RebuildInputHosts(field);
            RefreshAllInputTargeting(field);
            InputHostsChanged?.Invoke();
        }

        private bool IsCurrentGeneration(int generation) => generation == boardGeneration;

        public bool TryGetView(CardInstanceId id, out ICardBattleView view)
        {
            view = null;
            if (!id.IsValid)
            {
                return false;
            }

            if (boardSlots.TryGetValue(id, out var slot) && slot.Entity != null)
            {
                view = slot.Entity;
                return true;
            }

            return false;
        }

        public bool TryGetModel(CardInstanceId id, out CardModel model)
        {
            model = null;
            if (!id.IsValid)
            {
                return false;
            }

            if (boardSlots.TryGetValue(id, out var slot) && slot.Model != null)
            {
                model = slot.Model;
                return true;
            }

            return false;
        }

        private void RegisterEntity(CardModel model, CardEntity entity)
        {
            if (model == null || !model.InstanceId.IsValid || entity == null)
            {
                return;
            }

            boardSlots[model.InstanceId] = new BoardEntitySlot
            {
                Model = model,
                Entity = entity,
            };
        }

        private bool TryGetEntity(CardModel model, out CardEntity entity)
        {
            entity = null;
            if (model == null || !model.InstanceId.IsValid)
            {
                return false;
            }

            if (!boardSlots.TryGetValue(model.InstanceId, out var slot))
            {
                return false;
            }

            entity = slot.Entity;
            return entity != null;
        }

        private bool HasEntity(CardModel model)
        {
            return model != null
                && model.InstanceId.IsValid
                && boardSlots.ContainsKey(model.InstanceId);
        }

        private static void RefreshInputTargeting(BattleField field, CardModel model, CardEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            entity.ApplyInputTargeting(
                CardTargetingRules.CanBeginPlayerDrag(field, model),
                CardTargetingRules.CanAcceptBattlefieldTarget(field, model));
        }

        private void RefreshAllInputTargeting(BattleField field)
        {
            foreach (var pair in boardSlots)
            {
                RefreshInputTargeting(field, pair.Value.Model, pair.Value.Entity);
            }
        }

        private void SetEntityPhase(BattleField field, CardModel model, CardEntity entity, CardBoardPhase phase)
        {
            entity.SetPhase(phase);
            RefreshInputTargeting(field, model, entity);
        }

        private static void SnapCard(CardEntity entity, Vector3 localPosition)
        {
            entity.SnapToAnchorPose(localPosition);
        }

        private static UniTask DeployCard(
            CardEntity entity,
            Vector3 localPosition,
            float moveDuration,
            float flipDuration)
        {
            return entity.PlayDeployOnAnchorAsync(localPosition, moveDuration, flipDuration);
        }

        private static UniTask RealignCard(CardEntity entity, Vector3 localPosition, float moveDuration)
        {
            return entity.PlayRealignOnAnchorAsync(localPosition, moveDuration);
        }

        private async UniTask PlaceBattlefieldCardAsync(
            BattleField field,
            CardModel model,
            CardEntity entity,
            Vector3 localPosition,
            bool isNewToBattlefield,
            bool animate)
        {
            if (isNewToBattlefield)
            {
                if (animate)
                {
                    field?.MarkPendingBattlefieldDeploy(model);
                    RefreshInputTargeting(field, model, entity);
                    try
                    {
                        await DeployCard(entity, localPosition, layout.deployMoveDuration, layout.flipDuration);
                    }
                    finally
                    {
                        field?.ClearPendingBattlefieldDeploy(model);
                        RefreshInputTargeting(field, model, entity);
                    }
                }
                else
                {
                    SnapCard(entity, localPosition);
                    SetEntityPhase(field, model, entity, CardBoardPhase.BattlefieldFaceUp);
                }

                return;
            }

            if (animate && !entity.IsAtHomeLocalPosition(localPosition))
            {
                await RealignCard(entity, localPosition, layout.deployMoveDuration);
            }
            else
            {
                SnapCard(entity, localPosition);
            }

            SetEntityPhase(field, model, entity, CardBoardPhase.BattlefieldFaceUp);
        }

        private async UniTask SpawnTeam(
            BattleField field,
            CardModel[] battlefield,
            List<CardModel> reserve,
            Transform boardRoot,
            bool isPlayerTeam,
            int generation)
        {
            var zone = ResolveZoneLayout(boardRoot);
            if (zone == null)
            {
                return;
            }

            var allModels = FillModelBuffer(battlefield, reserve);
            for (var i = 0; i < allModels.Count; i++)
            {
                if (!IsCurrentGeneration(generation))
                {
                    return;
                }

                var model = allModels[i];
                var placement = ResolvePlacement(zone, model, battlefield, reserve, boardRoot);
                var entity = CreateEntity(model, placement.Parent, isPlayerTeam);
                RegisterEntity(model, entity);

                if (field.IsOnBattlefield(model))
                {
                    await PlaceBattlefieldCardAsync(
                        field,
                        model,
                        entity,
                        placement.LocalPosition,
                        isNewToBattlefield: true,
                        animate: true);
                }
                else
                {
                    await entity.SnapReserveOnAnchorAsync(placement.LocalPosition);
                    RefreshInputTargeting(field, model, entity);
                }
            }
        }

        private async UniTask SyncTeam(
            BattleField field,
            CardModel[] battlefield,
            List<CardModel> reserve,
            Transform boardRoot,
            bool isPlayerTeam,
            bool animateRefill,
            int generation)
        {
            var zone = ResolveZoneLayout(boardRoot);
            if (zone == null)
            {
                return;
            }

            var activeModels = FillModelBuffer(battlefield, reserve);
            modelSetBuffer.Clear();
            for (var i = 0; i < activeModels.Count; i++)
            {
                modelSetBuffer.Add(activeModels[i]);
            }

            foreach (var pair in boardSlots)
            {
                var model = pair.Value.Model;
                if (model == null || model.IsPlayerTeam != isPlayerTeam || modelSetBuffer.Contains(model))
                {
                    continue;
                }

                SetEntityPhase(field, model, pair.Value.Entity, CardBoardPhase.Hidden);
            }

            for (var i = 0; i < activeModels.Count; i++)
            {
                if (!IsCurrentGeneration(generation))
                {
                    return;
                }

                var model = activeModels[i];
                if (!TryGetEntity(model, out var entity))
                {
                    continue;
                }

                entity.SyncFromModel(model);
                RefreshInputTargeting(field, model, entity);
                var placement = ResolvePlacement(zone, model, battlefield, reserve, boardRoot);
                EnsureAnchorParent(entity, placement.Parent);

                if (field.IsOnBattlefield(model) && model.IsAlive)
                {
                    var isNewToBattlefield = entity.Phase != CardBoardPhase.BattlefieldFaceUp;
                    await PlaceBattlefieldCardAsync(
                        field,
                        model,
                        entity,
                        placement.LocalPosition,
                        isNewToBattlefield,
                        animateRefill);
                }
                else
                {
                    await entity.SnapReserveOnAnchorAsync(placement.LocalPosition);
                    RefreshInputTargeting(field, model, entity);
                }
            }

            if (!IsCurrentGeneration(generation))
            {
                return;
            }

            await EnsureEntitiesForTeam(field, battlefield, reserve, boardRoot, isPlayerTeam, animateRefill, generation);
        }

        private async UniTask EnsureEntitiesForTeam(
            BattleField field,
            CardModel[] battlefield,
            List<CardModel> reserve,
            Transform boardRoot,
            bool isPlayerTeam,
            bool animateRefill,
            int generation)
        {
            if (!IsCurrentGeneration(generation))
            {
                return;
            }

            var zone = ResolveZoneLayout(boardRoot);
            if (zone == null)
            {
                return;
            }

            var allModels = FillModelBuffer(battlefield, reserve);
            for (var i = 0; i < allModels.Count; i++)
            {
                if (!IsCurrentGeneration(generation))
                {
                    return;
                }

                var model = allModels[i];
                if (HasEntity(model))
                {
                    continue;
                }

                var placement = ResolvePlacement(zone, model, battlefield, reserve, boardRoot);
                var entity = CreateEntity(model, placement.Parent, isPlayerTeam);
                RegisterEntity(model, entity);
                entity.SyncFromModel(model);
                RefreshInputTargeting(field, model, entity);

                if (field.IsOnBattlefield(model))
                {
                    await PlaceBattlefieldCardAsync(
                        field,
                        model,
                        entity,
                        placement.LocalPosition,
                        isNewToBattlefield: true,
                        animate: animateRefill);
                }
                else
                {
                    await entity.SnapReserveOnAnchorAsync(placement.LocalPosition);
                    RefreshInputTargeting(field, model, entity);
                }
            }
        }

        private bool HasZoneLayouts()
        {
            return ResolveZoneLayout(playerBoardRoot) != null
                && ResolveZoneLayout(enemyBoardRoot) != null;
        }

        private BattleBoardZoneLayout ResolveZoneLayout(Transform boardRoot)
        {
            if (boardRoot == null)
            {
                Debug.LogError("[CardBattle] BoardRoot가 없습니다.");
                return null;
            }

            var zone = boardRoot.GetComponent<BattleBoardZoneLayout>();
            if (zone == null)
            {
                Debug.LogError(
                    $"[CardBattle] {boardRoot.name}에 BattleBoardZoneLayout이 없습니다. "
                    + "CardGame/CardBattle/Ensure Board Zone Anchors로 전장 앵커를 연결하세요.");
                return null;
            }

            if (zone.BattlefieldCenter == null)
            {
                Debug.LogError($"[CardBattle] {boardRoot.name} 전장 중심 앵커가 비어 있습니다.");
                return null;
            }

            return zone;
        }

        private readonly struct CardAnchorPlacement
        {
            public readonly Transform Parent;
            public readonly Vector3 LocalPosition;
            public readonly Quaternion LocalRotation;

            public CardAnchorPlacement(Transform parent, Vector3 localPosition, Quaternion localRotation)
            {
                Parent = parent;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
            }
        }

        private static CardAnchorPlacement ResolvePlacement(
            BattleBoardZoneLayout zone,
            CardModel model,
            CardModel[] battlefield,
            List<CardModel> reserve,
            Transform fallback)
        {
            if (IsInBattlefieldSlots(model, battlefield)
                && zone.TryComputeBattlefieldPose(model, battlefield, out var localPosition, out var localRotation))
            {
                return new CardAnchorPlacement(
                    zone.BattlefieldCenter != null ? zone.BattlefieldCenter : fallback,
                    localPosition,
                    localRotation);
            }

            var stackIndex = reserve.IndexOf(model);
            if (stackIndex < 0)
            {
                stackIndex = 0;
            }

            return new CardAnchorPlacement(
                zone.ReserveStackOrigin != null ? zone.ReserveStackOrigin : fallback,
                zone.GetReserveStackLocalOffset(stackIndex),
                Quaternion.identity);
        }

        private static void EnsureAnchorParent(CardEntity entity, Transform parent)
        {
            if (parent == null || entity == null || entity.transform.parent == parent)
            {
                return;
            }

            entity.transform.SetParent(parent, false);
        }

        private CardEntity CreateEntity(CardModel model, Transform parent, bool isPlayerTeam)
        {
            var entity = Instantiate(layout.cardEntityPrefab, parent);
            entity.name = $"{(isPlayerTeam ? "Player" : "Enemy")}_{model.DisplayName}";
            entity.ApplyLayout(layout);
            entity.SyncFromModel(model);
            entity.ApplyBackSprite(layout.GetCardBack(isPlayerTeam));
            return entity;
        }

        private void RebuildInputHosts(BattleField field)
        {
            inputHosts.Clear();
            AddBattlefieldInputHosts(field.PlayerBattlefield);
            AddBattlefieldInputHosts(field.EnemyBattlefield);
        }

        private void AddBattlefieldInputHosts(CardModel[] battlefield)
        {
            for (var i = 0; i < battlefield.Length; i++)
            {
                var model = battlefield[i];
                if (model == null || !model.IsAlive)
                {
                    continue;
                }

                if (TryGetEntity(model, out var entity) && entity.Phase == CardBoardPhase.BattlefieldFaceUp)
                {
                    inputHosts.Add(entity);
                }
            }
        }

        private List<CardModel> FillModelBuffer(CardModel[] battlefield, List<CardModel> reserve)
        {
            modelBuffer.Clear();
            for (var i = 0; i < battlefield.Length; i++)
            {
                if (battlefield[i] != null)
                {
                    modelBuffer.Add(battlefield[i]);
                }
            }

            for (var i = 0; i < reserve.Count; i++)
            {
                if (reserve[i] != null)
                {
                    modelBuffer.Add(reserve[i]);
                }
            }

            return modelBuffer;
        }

        private static bool IsInBattlefieldSlots(CardModel model, CardModel[] battlefield)
        {
            for (var i = 0; i < battlefield.Length; i++)
            {
                if (battlefield[i] == model)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
