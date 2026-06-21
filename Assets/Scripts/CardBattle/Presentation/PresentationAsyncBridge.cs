using System;
using Cysharp.Threading.Tasks;

namespace CardGame.CardBattle.Presentation
{
    internal static class PresentationAsyncBridge
    {
        public static UniTask FromCallback(Action<Action> invokeWithCallback)
        {
            if (invokeWithCallback == null)
            {
                return UniTask.CompletedTask;
            }

            var tcs = new UniTaskCompletionSource();
            invokeWithCallback(() => tcs.TrySetResult());
            return tcs.Task;
        }
    }
}
