using System;
using System.Collections.Generic;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;

namespace CardGame.CardBattle.Presentation
{
    public sealed class PresentationPlayer
    {
        private readonly Dictionary<int, HeroModel> heroLookup = new Dictionary<int, HeroModel>();

        public async UniTask PlayAsync(BattlePresentationSpec spec, PresentationSequence sequence)
        {
            if (spec == null || sequence == null)
            {
                return;
            }

            CacheHeroLookup(spec);
            PrepareHpDisplays(spec, sequence);

            var context = PresentationCueContext.ForBattle(spec, ResolveHero);
            for (var i = 0; i < sequence.Cues.Count; i++)
            {
                await PresentationCueDispatcher.ExecuteBattleAsync(context, sequence.Cues[i]);
            }
        }

        public UniTask PlayAttackAsync(PresentationContext context, PresentationSequence sequence)
        {
            if (context == null)
            {
                return UniTask.CompletedTask;
            }

            return PlayAsync(PresentationContext.ToSpec(context, 0f), sequence);
        }

        public async UniTask PlayTurnStartAsync(
            PresentationSequence sequence,
            CardGame.CardBattle.UI.UIManager ui,
            CardPresentationService presentation,
            ICardViewRegistry viewRegistry,
            HeroArenaPresenter heroPresenter = null)
        {
            if (sequence == null)
            {
                return;
            }

            PrepareTurnStartHpDisplays(sequence, viewRegistry, heroPresenter);

            var context = PresentationCueContext.ForTurnStart(ui, presentation, viewRegistry, heroPresenter);
            for (var i = 0; i < sequence.Cues.Count; i++)
            {
                if (TryCollectParallelHealFlights(sequence.Cues, i, out var batchEnd))
                {
                    await ExecuteParallelHealFlightsAsync(context, sequence.Cues, i, batchEnd);
                    i = batchEnd;
                    continue;
                }

                await PresentationCueDispatcher.ExecuteTurnStartAsync(context, sequence.Cues[i]);
            }
        }

        private static void PrepareTurnStartHpDisplays(
            PresentationSequence sequence,
            ICardViewRegistry viewRegistry,
            HeroArenaPresenter heroPresenter)
        {
            PrepareHpDisplaysFromCues(
                sequence?.Cues,
                (id, hpFrom) =>
                {
                    if (viewRegistry != null && viewRegistry.TryGetView(id, out var cardView))
                    {
                        cardView.SetHpDisplay(hpFrom);
                    }
                },
                (heroId, hpFrom) => heroPresenter?.SetHeroHpDisplay(heroId, hpFrom));
        }

        private static bool TryCollectParallelHealFlights(
            IReadOnlyList<PresentationCue> cues,
            int startIndex,
            out int batchEndExclusive)
        {
            batchEndExclusive = startIndex;
            if (startIndex >= cues.Count)
            {
                return false;
            }

            var first = cues[startIndex];
            if (first.Kind != PresentationCueKind.FlyProjectile
                || first.ProjectileRole != ProjectileRole.TurnHeal)
            {
                return false;
            }

            var index = startIndex + 1;
            while (index < cues.Count)
            {
                var cue = cues[index];
                if (cue.Kind != PresentationCueKind.FlyProjectile
                    || cue.ProjectileRole != ProjectileRole.TurnHeal)
                {
                    break;
                }

                index++;
            }

            batchEndExclusive = index - 1;
            return index > startIndex + 1;
        }

        private static async UniTask ExecuteParallelHealFlightsAsync(
            PresentationCueContext context,
            IReadOnlyList<PresentationCue> cues,
            int startIndex,
            int endIndexInclusive)
        {
            if (context.Presentation == null)
            {
                return;
            }

            var tasks = new List<UniTask>(endIndexInclusive - startIndex + 1);
            for (var i = startIndex; i <= endIndexInclusive; i++)
            {
                tasks.Add(PresentationCueDispatcher.ExecuteFlyProjectileAsync(context, cues[i]));
            }

            await UniTask.WhenAll(tasks);
        }

        private void CacheHeroLookup(BattlePresentationSpec spec)
        {
            heroLookup.Clear();
            RegisterHero(spec.PrimaryTargetHero);
            RegisterHero(spec.StrikerHero);
            RegisterHero(spec.DefenderHero);
        }

        private void RegisterHero(HeroModel hero)
        {
            if (hero != null && hero.InstanceId.IsValid)
            {
                heroLookup[hero.InstanceId.Value] = hero;
            }
        }

        private HeroModel ResolveHero(HeroInstanceId id)
        {
            if (!id.IsValid)
            {
                return null;
            }

            heroLookup.TryGetValue(id.Value, out var hero);
            return hero;
        }

        private static void PrepareHpDisplays(BattlePresentationSpec spec, PresentationSequence sequence)
        {
            PrepareHpDisplaysFromCues(
                sequence?.Cues,
                (id, hpFrom) => spec.GetCardView(id)?.SetHpDisplay(hpFrom),
                (heroId, hpFrom) => spec.HeroPresenter?.SetHeroHpDisplay(heroId, hpFrom));
        }

        private static void PrepareHpDisplaysFromCues(
            IReadOnlyList<PresentationCue> cues,
            Action<CardInstanceId, int> setCardHp,
            Action<HeroInstanceId, int> setHeroHp)
        {
            if (cues == null)
            {
                return;
            }

            var preparedCards = new HashSet<int>();
            var preparedHeroes = new HashSet<int>();

            for (var i = 0; i < cues.Count; i++)
            {
                var cue = cues[i];

                if (cue.Kind == PresentationCueKind.HpBarTween
                    && cue.SubjectId.IsValid
                    && cue.HpFrom >= 0
                    && preparedCards.Add(cue.SubjectId.Value))
                {
                    setCardHp?.Invoke(cue.SubjectId, cue.HpFrom);
                }

                if (cue.Kind == PresentationCueKind.HeroStatTween
                    && cue.SubjectHeroId.IsValid
                    && cue.HpFrom >= 0
                    && preparedHeroes.Add(cue.SubjectHeroId.Value))
                {
                    setHeroHp?.Invoke(cue.SubjectHeroId, cue.HpFrom);
                }
            }
        }
    }
}
