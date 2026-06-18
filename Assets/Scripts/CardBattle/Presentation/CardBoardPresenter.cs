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
    public sealed class CardBoardPresenter : MonoBehaviour
    {
        [SerializeField] private BattleLayoutConfig layout;
        [SerializeField] private Transform playerBoardRoot;
        [SerializeField] private Transform enemyBoardRoot;

        private readonly Dictionary<CardModel, CardEntity> entities = new Dictionary<CardModel, CardEntity>();
        private readonly List<ICardInputHost> inputHosts = new List<ICardInputHost>();
        private bool isBuilt;
        private int boardGeneration;
        private readonly SemaphoreSlim presentationLock = new SemaphoreSlim(1, 1);

        public event Action InputHostsChanged;

        public IReadOnlyList<ICardInputHost> InputHosts => inputHosts;

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

            foreach (var pair in entities)
            {
                if (pair.Value != null)
                {
                    pair.Value.CancelMotion();
                    Destroy(pair.Value.gameObject);
                }
            }

            entities.Clear();
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
                SpawnTeam(field.PlayerBattlefield, field.PlayerReserve, playerBoardRoot, true, generation),
                SpawnTeam(field.EnemyBattlefield, field.EnemyReserve, enemyBoardRoot, false, generation));

            if (!IsCurrentGeneration(generation))
            {
                return;
            }

            RebuildInputHosts(field);
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
                SyncTeam(field.PlayerBattlefield, field.PlayerReserve, playerBoardRoot, true, animateRefill, generation),
                SyncTeam(field.EnemyBattlefield, field.EnemyReserve, enemyBoardRoot, false, animateRefill, generation));

            if (!IsCurrentGeneration(generation))
            {
                return;
            }

            RebuildInputHosts(field);
            InputHostsChanged?.Invoke();
        }

        private bool IsCurrentGeneration(int generation) => generation == boardGeneration;

        public ICardBattleView FindView(CardModel model)
        {
            if (model == null)
            {
                return null;
            }

            return entities.TryGetValue(model, out var entity) ? entity : null;
        }

        private static void SnapCard(CardEntity entity)
        {
            entity.SnapToAnchorPose(Vector3.zero);
        }

        private static UniTask DeployCard(CardEntity entity, float moveDuration, float flipDuration)
        {
            return entity.PlayDeployOnAnchorAsync(Vector3.zero, moveDuration, flipDuration);
        }

        private async UniTask SpawnTeam(
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

            var allModels = CollectModels(battlefield, reserve);
            for (var i = 0; i < allModels.Count; i++)
            {
                if (!IsCurrentGeneration(generation))
                {
                    return;
                }

                var model = allModels[i];
                var placement = ResolvePlacement(zone, model, battlefield, reserve, boardRoot);
                var entity = CreateEntity(model, placement.Parent, isPlayerTeam);
                entities[model] = entity;

                if (IsOnBattlefield(model, battlefield))
                {
                    await DeployCard(entity, layout.deployMoveDuration, layout.flipDuration);
                }
                else
                {
                    await entity.SnapReserveOnAnchorAsync(placement.LocalPosition);
                }
            }
        }

        private async UniTask SyncTeam(
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

            var activeModels = CollectModels(battlefield, reserve);
            var activeSet = new HashSet<CardModel>(activeModels);

            foreach (var pair in entities)
            {
                var model = pair.Key;
                if (model == null || model.IsPlayerTeam != isPlayerTeam || activeSet.Contains(model))
                {
                    continue;
                }

                pair.Value.SetPhase(CardBoardPhase.Hidden);
            }

            for (var i = 0; i < activeModels.Count; i++)
            {
                if (!IsCurrentGeneration(generation))
                {
                    return;
                }

                var model = activeModels[i];
                if (!entities.TryGetValue(model, out var entity) || entity == null)
                {
                    continue;
                }

                entity.Bind(model);
                var placement = ResolvePlacement(zone, model, battlefield, reserve, boardRoot);
                EnsureAnchorParent(entity, placement.Parent);

                if (IsOnBattlefield(model, battlefield) && model.IsAlive)
                {
                    if (entity.Phase != CardBoardPhase.BattlefieldFaceUp)
                    {
                        if (animateRefill)
                        {
                            await DeployCard(entity, layout.deployMoveDuration, layout.flipDuration);
                        }
                        else
                        {
                            SnapCard(entity);
                            entity.SetPhase(CardBoardPhase.BattlefieldFaceUp);
                        }
                    }
                    else
                    {
                        SnapCard(entity);
                        entity.SetPhase(CardBoardPhase.BattlefieldFaceUp);
                    }
                }
                else
                {
                    await entity.SnapReserveOnAnchorAsync(placement.LocalPosition);
                }
            }

            if (!IsCurrentGeneration(generation))
            {
                return;
            }

            await EnsureEntitiesForTeam(battlefield, reserve, boardRoot, isPlayerTeam, animateRefill, generation);
        }

        private async UniTask EnsureEntitiesForTeam(
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

            var allModels = CollectModels(battlefield, reserve);
            for (var i = 0; i < allModels.Count; i++)
            {
                if (!IsCurrentGeneration(generation))
                {
                    return;
                }

                var model = allModels[i];
                if (entities.ContainsKey(model))
                {
                    continue;
                }

                var placement = ResolvePlacement(zone, model, battlefield, reserve, boardRoot);
                var entity = CreateEntity(model, placement.Parent, isPlayerTeam);
                entities[model] = entity;
                entity.Bind(model);

                if (IsOnBattlefield(model, battlefield))
                {
                    if (animateRefill)
                    {
                        await DeployCard(entity, layout.deployMoveDuration, layout.flipDuration);
                    }
                    else
                    {
                        SnapCard(entity);
                        entity.SetPhase(CardBoardPhase.BattlefieldFaceUp);
                    }
                }
                else
                {
                    await entity.SnapReserveOnAnchorAsync(placement.LocalPosition);
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
                    + "CardGame/CardBattle/Ensure Board Zone Anchors로 슬롯 앵커만 연결하세요.");
                return null;
            }

            if (zone.SlotCount == 0)
            {
                Debug.LogError($"[CardBattle] {boardRoot.name} 전장 슬롯 앵커가 비어 있습니다.");
                return null;
            }

            return zone;
        }

        private readonly struct CardAnchorPlacement
        {
            public readonly Transform Parent;
            public readonly Vector3 LocalPosition;

            public CardAnchorPlacement(Transform parent, Vector3 localPosition)
            {
                Parent = parent;
                LocalPosition = localPosition;
            }
        }

        private static CardAnchorPlacement ResolvePlacement(
            BattleBoardZoneLayout zone,
            CardModel model,
            CardModel[] battlefield,
            List<CardModel> reserve,
            Transform fallback)
        {
            if (IsOnBattlefield(model, battlefield))
            {
                var slot = Mathf.Clamp(model.SlotIndex, 0, zone.SlotCount - 1);
                return new CardAnchorPlacement(
                    zone.GetBattlefieldSlotAnchor(slot) ?? fallback,
                    Vector3.zero);
            }

            var stackIndex = reserve.IndexOf(model);
            if (stackIndex < 0)
            {
                stackIndex = 0;
            }

            return new CardAnchorPlacement(
                zone.ReserveStackOrigin != null ? zone.ReserveStackOrigin : fallback,
                zone.GetReserveStackLocalOffset(stackIndex));
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
            entity.Bind(model);
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

                if (entities.TryGetValue(model, out var entity) && entity.Phase == CardBoardPhase.BattlefieldFaceUp)
                {
                    inputHosts.Add(entity);
                }
            }
        }

        private static List<CardModel> CollectModels(CardModel[] battlefield, List<CardModel> reserve)
        {
            var list = new List<CardModel>(battlefield.Length + reserve.Count);
            for (var i = 0; i < battlefield.Length; i++)
            {
                if (battlefield[i] != null)
                {
                    list.Add(battlefield[i]);
                }
            }

            for (var i = 0; i < reserve.Count; i++)
            {
                if (reserve[i] != null)
                {
                    list.Add(reserve[i]);
                }
            }

            return list;
        }

        private static bool IsOnBattlefield(CardModel model, CardModel[] battlefield)
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
