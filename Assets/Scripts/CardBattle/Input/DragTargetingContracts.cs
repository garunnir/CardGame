using UnityEngine;

namespace CardGame.CardBattle.Input
{
    /// <summary>드래그 시작 주체를 공용 규약으로 노출한다.</summary>
    public interface IDragSource
    {
        object DragPayload { get; }
        Transform DragTransform { get; }
        bool CanBeginDrag { get; }
    }

    /// <summary>드롭 가능한 대상을 공용 규약으로 노출한다.</summary>
    public interface IDropTarget
    {
        object DropPayload { get; }
        Transform DropTransform { get; }
    }

    /// <summary>도메인 규칙으로 드래그/드롭 유효성을 판정한다.</summary>
    public interface IDragTargetingPolicy<TSource, TTarget, TAction>
    {
        bool CanStartDrag(TSource source);
        bool IsValidHover(TSource source, TTarget hoverTarget);
        bool TryBuildAction(TSource source, TTarget dropTarget, out TAction action);
    }

    /// <summary>호버 상태를 시각화할 수 있는 대상 컴포넌트.</summary>
    public interface IDragHoverVisual
    {
        void SetHoverState(bool isActive, bool isValid);
    }
}
