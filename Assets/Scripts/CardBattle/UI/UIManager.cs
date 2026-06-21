using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CardGame.CardBattle.UI
{
    /// <summary>턴 배너·덱 현황·결과 팝업·URP Volume 연동.</summary>
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private int sortingOrder = 100;
        [SerializeField] private TextMeshProUGUI turnBannerText;
        [SerializeField] private CanvasGroup turnBannerGroup;
        [SerializeField] private TextMeshProUGUI playerReserveText;
        [SerializeField] private TextMeshProUGUI enemyReserveText;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private float bloomHealIntensity = 1.8f;
        [SerializeField] private float bloomAttackIntensity = 1.2f;

        private float defaultBloomIntensity = 0.4f;
        private CancellationTokenSource bannerFadeCts;
        public event Action RestartRequested;

        private void Awake()
        {
            ApplySortingOrder();
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            HideResultPanels();
            CacheBloomDefault();
        }

        private void OnDestroy()
        {
            bannerFadeCts?.Cancel();
            bannerFadeCts?.Dispose();
            bannerFadeCts = null;

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
            }
        }

        public void ApplySortingOrder(int order = -1)
        {
            if (order >= 0)
            {
                sortingOrder = order;
            }

            if (rootCanvas != null)
            {
                rootCanvas.sortingOrder = sortingOrder;
            }
        }

        public void ShowTurnBanner(bool isPlayerTurn)
        {
            if (turnBannerText != null)
            {
                turnBannerText.text = isPlayerTurn ? "플레이어 턴" : "적 턴";
            }

            FadeBannerAsync(isPlayerTurn ? "플레이어 턴" : "적 턴").Forget();
        }

        public void ShowSkipBanner(string message)
        {
            FadeBannerAsync(string.IsNullOrEmpty(message) ? "턴 스킵" : message).Forget();
        }

        private async UniTaskVoid FadeBannerAsync(string text)
        {
            if (turnBannerText != null)
            {
                turnBannerText.text = text;
            }

            if (turnBannerGroup != null)
            {
                turnBannerGroup.alpha = 0f;
                bannerFadeCts?.Cancel();
                bannerFadeCts?.Dispose();
                bannerFadeCts = new CancellationTokenSource();
                await FadeBannerInternalAsync(bannerFadeCts.Token);
            }
        }

        private async UniTaskVoid FadeBannerAsync(CancellationToken token)
        {
            await FadeBannerInternalAsync(token);
        }

        private async UniTask FadeBannerInternalAsync(CancellationToken token)
        {
            const float duration = 0.35f;

            try
            {
                var elapsed = 0f;
                while (elapsed < duration)
                {
                    token.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    turnBannerGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                    await UniTask.Yield(token);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(0.8f), cancellationToken: token);

                elapsed = 0f;
                while (elapsed < duration)
                {
                    token.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    turnBannerGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                    await UniTask.Yield(token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void UpdateReserveCounts(int playerReserve, int enemyReserve)
        {
            if (playerReserveText != null)
            {
                playerReserveText.text = $"대기 {playerReserve}";
            }

            if (enemyReserveText != null)
            {
                enemyReserveText.text = $"대기 {enemyReserve}";
            }
        }

        public void ShowResultPopup(bool playerWin)
        {
            HideResultPanels();
            if (playerWin && winPanel != null)
            {
                winPanel.SetActive(true);
            }
            else if (!playerWin && losePanel != null)
            {
                losePanel.SetActive(true);
            }
        }

        public void HideResultPanels()
        {
            if (winPanel != null)
            {
                winPanel.SetActive(false);
            }

            if (losePanel != null)
            {
                losePanel.SetActive(false);
            }
        }

        private void OnRestartClicked()
        {
            HideResultPanels();
            RestartRequested?.Invoke();
        }

        public void PulseHealerBloom()
        {
            PulseBloom(bloomHealIntensity, 0.45f);
        }

        public void PulseAttackBloom()
        {
            PulseBloom(bloomAttackIntensity, 0.25f);
        }

        public void TriggerCameraShake(float strength)
        {
            // Cinemachine 미사용 — 메인 카메라 간단 셰이크 인터페이스
            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            cam.transform.DOShakePosition(0.2f, strength, 20, 90f, false, true);
        }

        private void CacheBloomDefault()
        {
            if (postProcessVolume == null || postProcessVolume.profile == null)
            {
                return;
            }

            if (postProcessVolume.profile.TryGet(out UnityEngine.Rendering.Universal.Bloom bloom))
            {
                defaultBloomIntensity = bloom.intensity.value;
            }
        }

        private void PulseBloom(float peak, float holdSeconds)
        {
            if (postProcessVolume == null || postProcessVolume.profile == null)
            {
                return;
            }

            if (!postProcessVolume.profile.TryGet(out UnityEngine.Rendering.Universal.Bloom bloom))
            {
                return;
            }

            DOTween.To(
                () => bloom.intensity.value,
                v => bloom.intensity.value = v,
                peak,
                0.12f).OnComplete(() =>
            {
                DOVirtual.DelayedCall(holdSeconds, () =>
                {
                    DOTween.To(
                        () => bloom.intensity.value,
                        v => bloom.intensity.value = v,
                        defaultBloomIntensity,
                        0.2f);
                });
            });
        }
    }
}
