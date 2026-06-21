using UnityEngine;
using UnityEngine.InputSystem;

namespace CardGame.CardBattle.Input
{
    /// <summary>클릭/드래그 위치에 이펙트를 스폰하고, 누르는 동안 따라간다.</summary>
    public sealed class ScreenClickFeedbackPresenter : MonoBehaviour
    {
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform spawnRoot;
        [SerializeField] private GameObject clickEffectPrefab;
        [SerializeField] private Vector3 spawnOffset;
        [SerializeField] private float autoDestroySeconds = 2f;
        [SerializeField] private float worldSpawnDistance = 10f;

        private GameObject activeInstance;
        private bool prefabIsUi;

        private Camera WorldCamera => rootCanvas != null && rootCanvas.worldCamera != null
            ? rootCanvas.worldCamera
            : Camera.main;

        private void Awake()
        {
            if (rootCanvas == null)
            {
                rootCanvas = GetComponent<Canvas>();
            }

            EnsureSpawnRoot();
            prefabIsUi = clickEffectPrefab != null && clickEffectPrefab.GetComponent<RectTransform>() != null;
        }

        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null || clickEffectPrefab == null)
            {
                return;
            }

            var screenPosition = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
            {
                BeginAt(screenPosition);
                return;
            }

            if (activeInstance == null)
            {
                return;
            }

            if (pointer.press.isPressed)
            {
                MoveTo(screenPosition);
                return;
            }

            if (pointer.press.wasReleasedThisFrame)
            {
                EndAt();
            }
        }

        private void BeginAt(Vector2 screenPosition)
        {
            if (activeInstance != null)
            {
                Destroy(activeInstance);
                activeInstance = null;
            }

            activeInstance = SpawnAt(screenPosition);
        }

        private void MoveTo(Vector2 screenPosition)
        {
            if (activeInstance == null)
            {
                return;
            }

            if (prefabIsUi)
            {
                SetUiPosition((RectTransform)activeInstance.transform, screenPosition);
                return;
            }

            if (TryScreenToWorld(screenPosition, out var worldPosition))
            {
                activeInstance.transform.position = worldPosition;
            }
        }

        private void EndAt()
        {
            var instance = activeInstance;
            activeInstance = null;

            if (instance == null)
            {
                return;
            }

            if (autoDestroySeconds > 0f)
            {
                Destroy(instance, autoDestroySeconds);
            }
            else
            {
                Destroy(instance);
            }
        }

        private GameObject SpawnAt(Vector2 screenPosition)
        {
            if (prefabIsUi)
            {
                EnsureSpawnRoot();
                if (spawnRoot == null)
                {
                    return null;
                }

                var instance = Instantiate(clickEffectPrefab, spawnRoot);
                SetUiPosition((RectTransform)instance.transform, screenPosition);
                return instance;
            }

            if (!TryScreenToWorld(screenPosition, out var spawnPosition))
            {
                return null;
            }

            return Instantiate(clickEffectPrefab, spawnPosition, Quaternion.identity);
        }

        private void SetUiPosition(RectTransform rect, Vector2 screenPosition)
        {
            if (TryScreenToLocal(screenPosition, out var localPoint))
            {
                rect.anchoredPosition = localPoint + (Vector2)spawnOffset;
            }
        }

        private bool TryScreenToLocal(Vector2 screenPosition, out Vector2 localPoint)
        {
            localPoint = default;
            if (spawnRoot == null)
            {
                return false;
            }

            var eventCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                spawnRoot,
                screenPosition,
                eventCamera,
                out localPoint);
        }

        private bool TryScreenToWorld(Vector2 screenPosition, out Vector3 worldPosition)
        {
            worldPosition = default;
            var camera = WorldCamera;
            if (camera == null)
            {
                return false;
            }

            var ray = camera.ScreenPointToRay(screenPosition);
            worldPosition = Physics.Raycast(ray, out var hit, 200f)
                ? hit.point
                : ray.GetPoint(worldSpawnDistance);
            worldPosition += spawnOffset;
            return true;
        }

        private void EnsureSpawnRoot()
        {
            if (spawnRoot != null || rootCanvas == null)
            {
                return;
            }

            var go = new GameObject("ClickFeedbackRoot", typeof(RectTransform));
            go.transform.SetParent(rootCanvas.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            spawnRoot = rect;
        }
    }
}
