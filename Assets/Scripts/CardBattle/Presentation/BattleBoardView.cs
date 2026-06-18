using Sirenix.OdinInspector;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>BattleBoard 뷰 포즈를 Inspector 값으로만 적용.</summary>
    public sealed class BattleBoardView : MonoBehaviour
    {
        [SerializeField] private Camera battleCamera;
        [SerializeField] private Transform boardPlane;
        [SerializeField] private bool applyBoardRootPose;
        [SerializeField] private bool applyBoardPlanePose;
        [SerializeField] private bool applyCameraPose;
        [SerializeField] private Vector3 boardRootLocalPosition;
        [SerializeField] private Vector3 boardRootLocalEuler;
        [SerializeField] private Vector3 boardPlaneLocalPosition;
        [SerializeField] private Vector3 boardPlaneLocalEuler;
        [SerializeField] private Vector3 cameraWorldPosition;
        [SerializeField] private Vector3 cameraWorldEuler;

        private void Reset()
        {
            boardPlane = transform.Find("BoardPlane");
            battleCamera = Camera.main;
        }

        public void ApplyView()
        {
            if (boardPlane == null)
            {
                boardPlane = transform.Find("BoardPlane");
            }

            if (battleCamera == null)
            {
                battleCamera = Camera.main;
            }

            if (applyBoardRootPose)
            {
                transform.localPosition = boardRootLocalPosition;
                transform.localRotation = Quaternion.Euler(boardRootLocalEuler);
            }

            if (applyBoardPlanePose && boardPlane != null)
            {
                boardPlane.localPosition = boardPlaneLocalPosition;
                boardPlane.localRotation = Quaternion.Euler(boardPlaneLocalEuler);
            }

            if (applyCameraPose && battleCamera != null)
            {
                battleCamera.transform.position = cameraWorldPosition;
                battleCamera.transform.rotation = Quaternion.Euler(cameraWorldEuler);
            }
        }

#if UNITY_EDITOR
        [Button("Apply Battle View", ButtonSizes.Medium)]
        private void ApplyViewEditor()
        {
            ApplyView();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            if (battleCamera != null)
            {
                UnityEditor.EditorUtility.SetDirty(battleCamera.gameObject);
            }
        }
#endif
    }
}
