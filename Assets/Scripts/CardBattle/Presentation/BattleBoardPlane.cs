using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>전장 테이블 XZ 평면 — Scene 뷰 기준면.</summary>
    public sealed class BattleBoardPlane : MonoBehaviour
    {
        [SerializeField] private float gizmoHalfWidth = 4.5f;
        [SerializeField] private float gizmoHalfDepth = 5f;
        [SerializeField] private float gizmoSurfaceOffset;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.85f, 0.75f, 0.45f, 0.35f);
            BattleBoardGizmos.DrawFlatRect(
                transform,
                transform.position,
                gizmoHalfWidth * 2f,
                gizmoHalfDepth * 2f);

            Gizmos.color = new Color(0.85f, 0.75f, 0.45f, 0.12f);
            BattleBoardGizmos.DrawFlatGrid(
                transform,
                transform.position,
                gizmoHalfWidth * 2f,
                gizmoHalfDepth * 2f,
                divisions: 4);

            Gizmos.color = new Color(0.2f, 0.85f, 0.35f, 0.6f);
            var flatCenter = transform.TransformPoint(new Vector3(0f, gizmoSurfaceOffset, 0f));
            Gizmos.DrawWireSphere(flatCenter, 0.05f);
        }
#endif
    }
}
