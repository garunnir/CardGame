using System;
using System.Collections.Generic;
using System.Threading;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>3D 카드 보드 — 레지스트리·lock·팀 스폰/동기화 오케스트레이션.</summary>
    public sealed class CardBoardPresenter : MonoBehaviour, ICardViewRegistry, ICardBoardSession
    {
        [SerializeField] private BattleLayoutConfig layout;
        [SerializeField] private Transform playerBoardRoot;
        [SerializeField] private Transform enemyBoardRoot;

        private readonly CardBoardEntityRegistry registry = new CardBoardEntityRegistry();
        private readonly List<ICardInputHost> inputHosts = new List<ICardInputHost>();
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

            foreach (var pair in registry.Entries)
            {
                if (pair.Value.Entity != null)
                {
                    pair.Value.Entity.CancelMotion();
                    Destroy(pair.Value.Entity.gameObject);
                }
            }

            registry.Clear();
            inputHosts.Clear();
            isBuilt = false;
        }

        public UniTask BuildBoardAsync(BattleField field)
        {
            if (!CanRunBoardOps())
            {
                return UniTask.CompletedTask;
            }

            return EnqueuePresentation(() => BuildBoardInternalAsync(field));
        }

        public UniTask SyncBoardAsync(BattleField field, bool animateRefill)
        {
            if (!CanRunBoardOps())
            {
                return UniTask.CompletedTask;
            }

            return EnqueuePresentation(() => SyncBoardInternalAsync(field, animateRefill));
        }

        public UniTask SyncBoardWithinLockAsync(BattleField field, bool animateRefill)
        {
            if (!CanRunBoardOps())
            {
                return UniTask.CompletedTask;
            }

            return SyncBoardInternalAsync(field, animateRefill);
        }

        public UniTask RunExclusiveAsync(Func<UniTask> work)
        {
            return EnqueuePresentation(work);
        }

        public bool TryGetView(CardInstanceId id, out ICardBattleView view)
        {
            return registry.TryGetView(id, out view);
        }

        public bool TryGetModel(CardInstanceId id, out CardModel model)
        {
            return registry.TryGetModel(id, out model);
        }

        private bool CanRunBoardOps()
        {
            if (layout == null || layout.cardEntityPrefab == null)
            {
                Debug.LogError("[CardBattle] BattleLayoutConfig 또는 CardEntity 프리팹이 없습니다.");
                return false;
            }

            if (!CardBoardPlacement.HasZoneLayouts(playerBoardRoot, enemyBoardRoot))
            {
                return false;
            }

            return true;
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
            var createEntity = (Func<CardModel, Transform, bool, CardEntity>)CreateEntity;
            var isCurrentGeneration = (Func<int, bool>)IsCurrentGeneration;

            await CardBoardTeamOps.SpawnTeamAsync(
                field, registry, isCurrentGeneration, createEntity,
                field.PlayerBattlefield, field.PlayerReserve, playerBoardRoot, true, generation);
            await CardBoardTeamOps.SpawnTeamAsync(
                field, registry, isCurrentGeneration, createEntity,
                field.EnemyBattlefield, field.EnemyReserve, enemyBoardRoot, false, generation);

            if (!IsCurrentGeneration(generation))
            {
                return;
            }

            FinalizeBoardState(field);
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
            var createEntity = (Func<CardModel, Transform, bool, CardEntity>)CreateEntity;
            var isCurrentGeneration = (Func<int, bool>)IsCurrentGeneration;

            await CardBoardTeamOps.SyncTeamAsync(
                field, registry, isCurrentGeneration, createEntity,
                field.PlayerBattlefield, field.PlayerReserve, playerBoardRoot, true, animateRefill, generation);
            await CardBoardTeamOps.SyncTeamAsync(
                field, registry, isCurrentGeneration, createEntity,
                field.EnemyBattlefield, field.EnemyReserve, enemyBoardRoot, false, animateRefill, generation);

            if (!IsCurrentGeneration(generation))
            {
                return;
            }

            FinalizeBoardState(field);
            InputHostsChanged?.Invoke();
        }

        private void FinalizeBoardState(BattleField field)
        {
            RebuildInputHosts(field);
            CardBoardInputTargeting.RefreshAll(field, registry);
        }

        private bool IsCurrentGeneration(int generation) => generation == boardGeneration;

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

                if (registry.TryGetEntity(model, out var entity) && entity.Phase == CardBoardPhase.BattlefieldFaceUp)
                {
                    inputHosts.Add(entity);
                }
            }
        }
    }
}
