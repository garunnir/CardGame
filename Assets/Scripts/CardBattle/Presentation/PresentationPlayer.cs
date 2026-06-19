using System;
using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;

namespace CardGame.CardBattle.Presentation
{
    public sealed class PresentationPlayer
    {
        public async UniTask PlayAttackAsync(
            PresentationContext context,
            PresentationSequence sequence)
        {
            if (context == null || sequence == null)
            {
                return;
            }

            PrepareHpDisplays(context, sequence);

            for (var i = 0; i < sequence.Cues.Count; i++)
            {
                await ExecuteCueAsync(context, sequence.Cues[i]);
            }
        }

        public async UniTask PlayTurnStartAsync(
            PresentationSequence sequence,
            CardGame.CardBattle.UI.UIManager ui,
            CardPresentationService presentation,
            ICardViewRegistry viewRegistry)
        {
            if (sequence == null)
            {
                return;
            }

            for (var i = 0; i < sequence.Cues.Count; i++)
            {
                await ExecuteTurnCueAsync(sequence.Cues[i], ui, presentation, viewRegistry);
            }
        }

        private static void PrepareHpDisplays(PresentationContext context, PresentationSequence sequence)
        {
            var prepared = new HashSet<int>();
            for (var i = 0; i < sequence.Cues.Count; i++)
            {
                var cue = sequence.Cues[i];
                if (cue.Kind != PresentationCueKind.HpBarTween
                    || !cue.SubjectId.IsValid
                    || cue.HpFrom < 0
                    || !prepared.Add(cue.SubjectId.Value))
                {
                    continue;
                }

                var view = context.GetView(cue.SubjectId);
                view?.SetHpDisplay(cue.HpFrom);
            }
        }

        private async UniTask ExecuteTurnCueAsync(
            PresentationCue cue,
            CardGame.CardBattle.UI.UIManager ui,
            CardPresentationService presentation,
            ICardViewRegistry viewRegistry)
        {
            switch (cue.Kind)
            {
                case PresentationCueKind.UiHealerBloom:
                    ui?.PulseHealerBloom();
                    break;

                case PresentationCueKind.PlayTurnHealPresentation:
                    if (viewRegistry != null
                        && viewRegistry.TryGetModel(cue.SubjectId, out var healer))
                    {
                        viewRegistry.TryGetView(cue.SubjectId, out var view);
                        presentation?.PlayTurnHeal(healer, view);
                    }

                    break;

                case PresentationCueKind.HpBarTween:
                    if (viewRegistry != null && viewRegistry.TryGetView(cue.SubjectId, out var healTarget))
                    {
                        healTarget.SetHpDisplay(cue.HpFrom);
                        await PlayHpBarTweenAsync(healTarget, cue.HpFrom, cue.HpTo);
                    }

                    break;

                case PresentationCueKind.Wait:
                    await UniTask.Delay(TimeSpan.FromSeconds(cue.Duration));
                    break;
            }
        }

        private async UniTask ExecuteCueAsync(PresentationContext context, PresentationCue cue)
        {
            switch (cue.Kind)
            {
                case PresentationCueKind.Wait:
                case PresentationCueKind.WaitBeforeSecondary:
                    await UniTask.Delay(TimeSpan.FromSeconds(cue.Duration));
                    break;

                case PresentationCueKind.UiAttackBloom:
                    context.Ui?.PulseAttackBloom();
                    break;

                case PresentationCueKind.PlayAttackPresentation:
                    context.Presentation?.PlayAttack(
                        context.Attacker,
                        context.GetView(context.Attacker.InstanceId));
                    break;

                case PresentationCueKind.PlayShootPresentation:
                    context.Presentation?.PlayAttack(
                        context.Attacker,
                        context.GetView(context.Attacker.InstanceId));
                    break;

                case PresentationCueKind.AttackDash:
                    await PlayAttackDashAsync(context, cue.Duration);
                    break;

                case PresentationCueKind.PlayHitPresentation:
                    context.Presentation?.PlayHit(
                        context.Attacker,
                        context.GetView(context.Target.InstanceId));
                    break;

                case PresentationCueKind.HitShake:
                    await PlayHitShakeAsync(context.GetView(context.Target.InstanceId), cue.FloatParam);
                    break;

                case PresentationCueKind.HpBarTween:
                    await PlayHpBarTweenAsync(context.GetView(cue.SubjectId), cue.HpFrom, cue.HpTo);
                    break;

                case PresentationCueKind.PlayCounterPresentation:
                    context.Presentation?.PlayCounter(
                        context.Attacker,
                        context.GetView(context.Attacker.InstanceId));
                    break;

                case PresentationCueKind.PlaySecondaryHitPresentation:
                    context.Presentation?.PlayMusouSecondaryHit(
                        context.Attacker,
                        context.GetView(cue.SubjectId));
                    break;

                case PresentationCueKind.CameraShake:
                    context.Ui?.TriggerCameraShake(cue.FloatParam);
                    break;

                case PresentationCueKind.PlayDeathPresentation:
                    await PlayDeathPresentationAsync(context, cue.SubjectId);
                    break;

                default:
                    break;
            }
        }

        private static async UniTask PlayAttackDashAsync(PresentationContext context, float dashDuration)
        {
            var attackerView = context.GetView(context.Attacker.InstanceId);
            var targetView = context.GetView(context.Target.InstanceId);
            if (attackerView == null || targetView == null)
            {
                return;
            }

            var tcs = new UniTaskCompletionSource();
            attackerView.PlayAttackDash(
                targetView.ViewTransform.position,
                dashDuration,
                null,
                () => tcs.TrySetResult());
            await tcs.Task;
        }

        private static async UniTask PlayHitShakeAsync(ICardBattleView view, float strength)
        {
            if (view == null)
            {
                return;
            }

            var tcs = new UniTaskCompletionSource();
            view.PlayHitShake(strength, () => tcs.TrySetResult());
            await tcs.Task;
        }

        private static async UniTask PlayHpBarTweenAsync(ICardBattleView view, int fromHp, int toHp)
        {
            if (view == null || fromHp < 0 || toHp < 0)
            {
                return;
            }

            var tcs = new UniTaskCompletionSource();
            view.PlayHpChange(fromHp, toHp, () => tcs.TrySetResult());
            await tcs.Task;
        }

        private static async UniTask PlayDeathPresentationAsync(PresentationContext context, CardInstanceId subjectId)
        {
            if (!subjectId.IsValid || !context.ViewRegistry.TryGetModel(subjectId, out var subject))
            {
                return;
            }

            var view = context.GetView(subjectId);
            context.Presentation?.PlayDeath(subject, view);
            if (view == null)
            {
                return;
            }

            var tcs = new UniTaskCompletionSource();
            view.PlayDeathVisual(() => tcs.TrySetResult());
            await tcs.Task;
        }
    }
}
