using System;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;

namespace CardGame.CardBattle.Presentation
{
    public sealed class PresentationPlayer
    {
        public async UniTask<BattleActionResult> PlayAttackAsync(
            PresentationContext context,
            PresentationSequence sequence)
        {
            if (context == null || sequence == null)
            {
                return default;
            }

            for (var i = 0; i < sequence.Cues.Count; i++)
            {
                await ExecuteCueAsync(context, sequence.Cues[i]);
            }

            return new BattleActionResult(
                context.Attacker,
                context.Target,
                context.AppliedPrimaryDamage,
                context.AppliedCounterDamage,
                context.AppliedSecondary);
        }

        public async UniTask PlayTurnStartAsync(
            PresentationSequence sequence,
            CardGame.CardBattle.UI.UIManager ui,
            CardPresentationService presentation,
            Func<CardModel, CardView> findView)
        {
            if (sequence == null)
            {
                return;
            }

            for (var i = 0; i < sequence.Cues.Count; i++)
            {
                await ExecuteTurnCueAsync(sequence.Cues[i], ui, presentation, findView);
            }
        }

        private async UniTask ExecuteTurnCueAsync(
            PresentationCue cue,
            CardGame.CardBattle.UI.UIManager ui,
            CardPresentationService presentation,
            Func<CardModel, CardView> findView)
        {
            switch (cue.Kind)
            {
                case PresentationCueKind.UiHealerBloom:
                    ui?.PulseHealerBloom();
                    break;

                case PresentationCueKind.PlayTurnHealPresentation:
                    presentation?.PlayTurnHeal(cue.Subject, findView?.Invoke(cue.Subject));
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
                        context.FindView(context.Attacker));
                    break;

                case PresentationCueKind.PlayShootPresentation:
                    context.Presentation?.PlayAttack(
                        context.Attacker,
                        context.FindView(context.Attacker));
                    break;

                case PresentationCueKind.AttackDash:
                    await PlayAttackDashAsync(context, cue.Duration);
                    break;

                case PresentationCueKind.ApplyPrimaryDamage:
                    ApplyPrimaryDamage(context);
                    break;

                case PresentationCueKind.PlayHitPresentation:
                    context.Presentation?.PlayHit(
                        context.Attacker,
                        context.FindView(context.Target));
                    break;

                case PresentationCueKind.HitShake:
                    await PlayHitShakeAsync(context.FindView(context.Target), cue.FloatParam);
                    break;

                case PresentationCueKind.HpBarTween:
                    await PlayHpBarTweenAsync(
                        context.FindView(cue.Subject),
                        cue.Subject,
                        GetBeforeHp(context, cue.Subject));
                    break;

                case PresentationCueKind.PlayCounterPresentation:
                    context.Presentation?.PlayCounter(
                        context.Attacker,
                        context.FindView(context.Attacker));
                    break;

                case PresentationCueKind.ApplyCounterDamage:
                    ApplyCounterDamage(context);
                    break;

                case PresentationCueKind.PlaySecondaryHitPresentation:
                    context.Presentation?.PlayMusouSecondaryHit(
                        context.Attacker,
                        context.FindView(cue.Subject));
                    break;

                case PresentationCueKind.ApplySecondaryDamage:
                    ApplySecondaryDamage(context);
                    break;

                case PresentationCueKind.CameraShake:
                    context.Ui?.TriggerCameraShake(cue.FloatParam);
                    break;

                default:
                    break;
            }
        }

        private async UniTask PlayAttackDashAsync(PresentationContext context, float dashDuration)
        {
            var attackerView = context.FindView(context.Attacker);
            var targetView = context.FindView(context.Target);
            if (attackerView == null || targetView == null)
            {
                ApplyPrimaryDamage(context);
                return;
            }

            var tcs = new UniTaskCompletionSource();
            attackerView.PlayAttackDash(
                targetView.transform.position,
                dashDuration,
                () => ApplyPrimaryDamage(context),
                () => tcs.TrySetResult());
            await tcs.Task;
        }

        private static async UniTask PlayHitShakeAsync(CardView view, float strength)
        {
            if (view == null)
            {
                return;
            }

            var tcs = new UniTaskCompletionSource();
            view.PlayHitShake(strength, () => tcs.TrySetResult());
            await tcs.Task;
        }

        private static async UniTask PlayHpBarTweenAsync(CardView view, CardModel model, int beforeHp)
        {
            if (view == null || model == null)
            {
                return;
            }

            var tcs = new UniTaskCompletionSource();
            view.PlayHpChange(beforeHp, model.CurrentHp, () => tcs.TrySetResult());
            await tcs.Task;
        }

        private static int GetBeforeHp(PresentationContext context, CardModel subject)
        {
            if (subject == context.Attacker)
            {
                return context.BeforeAttackerHp;
            }

            if (subject == context.Target)
            {
                return context.BeforeTargetHp;
            }

            if (context.Resolution.Secondary.HasTarget
                && subject == context.Resolution.Secondary.Target)
            {
                return subject.CurrentHp + context.Resolution.Secondary.Damage;
            }

            return subject.CurrentHp;
        }

        private static void ApplyPrimaryDamage(PresentationContext context)
        {
            if (context.AppliedPrimaryDamage > 0)
            {
                return;
            }

            var damage = context.Resolution.PrimaryDamage;
            if (damage <= 0)
            {
                return;
            }

            context.Target.ApplyDamage(damage);
            context.AppliedPrimaryDamage = damage;
        }

        private static void ApplyCounterDamage(PresentationContext context)
        {
            if (context.Resolution.CounterDamage <= 0 || !context.Target.IsAlive)
            {
                return;
            }

            var damage = context.Resolution.CounterDamage;
            context.Attacker.ApplyDamage(damage);
            context.AppliedCounterDamage = damage;
        }

        private static void ApplySecondaryDamage(PresentationContext context)
        {
            var secondary = context.Resolution.Secondary;
            if (!secondary.HasTarget)
            {
                return;
            }

            secondary.Target.ApplyDamage(secondary.Damage);
            context.AppliedSecondary = secondary;
        }
    }
}
