using CardGame.CardBattle.Core;
using CardGame.CardBattle.UI;
using UnityEngine;

namespace CardGame.CardBattle.Core
{
    /// <summary>씬에서 GameManager·UIManager·BattleBridge 연결.</summary>
    public sealed class BattleSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private Bridge.BattleBridge battleBridge;

        private void Awake()
        {
            if (uiManager != null && gameManager != null)
            {
                uiManager.RestartRequested += OnRestart;
            }
        }

        private void OnDestroy()
        {
            if (uiManager != null)
            {
                uiManager.RestartRequested -= OnRestart;
            }
        }

        private void OnRestart()
        {
            gameManager?.RestartBattle();
        }
    }
}
