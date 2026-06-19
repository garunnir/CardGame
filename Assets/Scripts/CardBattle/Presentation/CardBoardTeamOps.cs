using System;
using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    internal static class CardBoardTeamOps
    {
        public static void FillModelBuffer(
            CardModel[] battlefield,
            List<CardModel> reserve,
            List<CardModel> buffer)
        {
            buffer.Clear();
            for (var i = 0; i < battlefield.Length; i++)
            {
                if (battlefield[i] != null)
                {
                    buffer.Add(battlefield[i]);
                }
            }

            for (var i = 0; i < reserve.Count; i++)
            {
                if (reserve[i] != null)
                {
                    buffer.Add(reserve[i]);
                }
            }
        }

        public static async UniTask SpawnTeamAsync(
            BattleField field,
            CardBoardEntityRegistry registry,
            Func<int, bool> isCurrentGeneration,
            Func<CardModel, Transform, bool, CardEntity> createEntity,
            CardModel[] battlefield,
            List<CardModel> reserve,
            Transform boardRoot,
            bool isPlayerTeam,
            int generation)
        {
            var zone = CardBoardPlacement.ResolveZoneLayout(boardRoot);
            if (zone == null)
            {
                return;
            }

            var models = new List<CardModel>(16);
            FillModelBuffer(battlefield, reserve, models);

            for (var i = 0; i < models.Count; i++)
            {
                if (!isCurrentGeneration(generation))
                {
                    return;
                }

                var model = models[i];
                var placement = CardBoardPlacement.ResolveReserveStackPlacement(zone, i, boardRoot);
                var entity = createEntity(model, placement.Parent, isPlayerTeam);
                registry.Register(model, entity);
                await PlaceReserveCardAsync(field, model, entity, placement, animate: false);
            }

            for (var slot = 0; slot < battlefield.Length; slot++)
            {
                if (!isCurrentGeneration(generation))
                {
                    return;
                }

                var model = battlefield[slot];
                if (model == null || !registry.TryGetEntity(model, out var entity))
                {
                    continue;
                }

                var placement = CardBoardPlacement.ResolvePlacement(zone, model, battlefield, reserve, boardRoot);
                await PlaceBattlefieldCardAsync(
                    field,
                    model,
                    entity,
                    placement,
                    isNewToBattlefield: true,
                    animate: true);
            }
        }

        public static async UniTask SyncTeamAsync(
            BattleField field,
            CardBoardEntityRegistry registry,
            Func<int, bool> isCurrentGeneration,
            Func<CardModel, Transform, bool, CardEntity> createEntity,
            CardModel[] battlefield,
            List<CardModel> reserve,
            Transform boardRoot,
            bool isPlayerTeam,
            bool animateRefill,
            int generation)
        {
            var zone = CardBoardPlacement.ResolveZoneLayout(boardRoot);
            if (zone == null)
            {
                return;
            }

            var models = new List<CardModel>(16);
            var activeModels = new HashSet<CardModel>();
            FillModelBuffer(battlefield, reserve, models);
            for (var i = 0; i < models.Count; i++)
            {
                activeModels.Add(models[i]);
            }

            foreach (var pair in registry.Entries)
            {
                var model = pair.Value.Model;
                if (model == null || model.IsPlayerTeam != isPlayerTeam || activeModels.Contains(model))
                {
                    continue;
                }

                CardBoardInputTargeting.SetPhase(field, model, pair.Value.Entity, CardBoardPhase.Hidden);
            }

            for (var i = 0; i < models.Count; i++)
            {
                if (!isCurrentGeneration(generation))
                {
                    return;
                }

                var model = models[i];
                if (!registry.TryGetEntity(model, out var entity))
                {
                    continue;
                }

                entity.SyncFromModel(model);
                CardBoardInputTargeting.Refresh(field, model, entity);
                var placement = CardBoardPlacement.ResolvePlacement(zone, model, battlefield, reserve, boardRoot);

                if (field.IsOnBattlefield(model) && model.IsAlive)
                {
                    var isNewToBattlefield = entity.Phase != CardBoardPhase.BattlefieldFaceUp;
                    await PlaceBattlefieldCardAsync(
                        field,
                        model,
                        entity,
                        placement,
                        isNewToBattlefield,
                        animateRefill);
                }
                else
                {
                    await PlaceReserveCardAsync(
                        field,
                        model,
                        entity,
                        placement,
                        animate: animateRefill);
                }
            }

            if (!isCurrentGeneration(generation))
            {
                return;
            }

            await EnsureEntitiesForTeamAsync(
                field,
                registry,
                models,
                isCurrentGeneration,
                createEntity,
                battlefield,
                reserve,
                boardRoot,
                isPlayerTeam,
                animateRefill,
                generation);
        }

        private static async UniTask PlaceBattlefieldCardAsync(
            BattleField field,
            CardModel model,
            CardEntity entity,
            CardBoardPlacement.AnchorPlacement placement,
            bool isNewToBattlefield,
            bool animate)
        {
            var isDeployAnimate = isNewToBattlefield && animate;

            if (isDeployAnimate)
            {
                field?.MarkPendingBattlefieldDeploy(model);
                CardBoardInputTargeting.Refresh(field, model, entity);
            }

            try
            {
                ICardBoardMotion motion = entity;
                await motion.ApplyPlacement(
                    placement.Parent,
                    placement.LocalPosition,
                    placement.LocalRotation,
                    CardBoardPhase.BattlefieldFaceUp,
                    animate);
            }
            finally
            {
                if (isDeployAnimate)
                {
                    field?.ClearPendingBattlefieldDeploy(model);
                }

                CardBoardInputTargeting.Refresh(field, model, entity);
            }
        }

        private static async UniTask PlaceReserveCardAsync(
            BattleField field,
            CardModel model,
            CardEntity entity,
            CardBoardPlacement.AnchorPlacement placement,
            bool animate)
        {
            ICardBoardMotion motion = entity;
            await motion.ApplyPlacement(
                placement.Parent,
                placement.LocalPosition,
                placement.LocalRotation,
                CardBoardPhase.ReserveFaceDown,
                animate);
            CardBoardInputTargeting.Refresh(field, model, entity);
        }

        private static async UniTask EnsureEntitiesForTeamAsync(
            BattleField field,
            CardBoardEntityRegistry registry,
            List<CardModel> models,
            Func<int, bool> isCurrentGeneration,
            Func<CardModel, Transform, bool, CardEntity> createEntity,
            CardModel[] battlefield,
            List<CardModel> reserve,
            Transform boardRoot,
            bool isPlayerTeam,
            bool animateRefill,
            int generation)
        {
            if (!isCurrentGeneration(generation))
            {
                return;
            }

            var zone = CardBoardPlacement.ResolveZoneLayout(boardRoot);
            if (zone == null)
            {
                return;
            }

            for (var i = 0; i < models.Count; i++)
            {
                if (!isCurrentGeneration(generation))
                {
                    return;
                }

                var model = models[i];
                if (registry.HasEntity(model))
                {
                    continue;
                }

                var placement = CardBoardPlacement.ResolvePlacement(zone, model, battlefield, reserve, boardRoot);
                var entity = createEntity(model, placement.Parent, isPlayerTeam);
                registry.Register(model, entity);
                entity.SyncFromModel(model);
                CardBoardInputTargeting.Refresh(field, model, entity);

                if (field.IsOnBattlefield(model))
                {
                    await PlaceBattlefieldCardAsync(
                        field,
                        model,
                        entity,
                        placement,
                        isNewToBattlefield: true,
                        animate: animateRefill);
                }
                else
                {
                    await PlaceReserveCardAsync(
                        field,
                        model,
                        entity,
                        placement,
                        animate: animateRefill);
                }
            }
        }
    }
}
