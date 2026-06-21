using System;
using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace CardGame.CardBattle.Bridge
{
    /// <summary>메인 프로젝트 ↔ CardBattle 모듈 진입점.</summary>
    public sealed class BattleBridge : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private CardDataAsset[] defaultPlayerDeck = new CardDataAsset[6];
        [SerializeField] private CardDataAsset[] defaultEnemyDeck = new CardDataAsset[6];
        [SerializeField] private HeroDataAsset defaultPlayerHero;
        [SerializeField] private HeroDataAsset defaultEnemyHero;
        [SerializeField] private BattleAudioAdapter audioAdapter;

        private ICardDataLoader cardDataLoader;

        public void Configure(
            GameManager manager,
            ICardDataLoader loader,
            BattleAudioAdapter audio)
        {
            gameManager = manager;
            cardDataLoader = loader;
            audioAdapter = audio;
            if (gameManager != null && audioAdapter != null)
            {
                gameManager.ConfigurePresentation(audioAdapter);
            }
        }

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GetComponent<GameManager>();
            }

            if (audioAdapter == null)
            {
                audioAdapter = GetComponent<BattleAudioAdapter>();
            }

            if (gameManager != null && audioAdapter != null)
            {
                gameManager.ConfigurePresentation(audioAdapter);
            }
        }

        private void Start()
        {
            StartBattleFromDefaults();
        }

        /// <summary>외부 ID 목록으로 덱 구성 후 전투 시작.</summary>
        public void StartBattleFromIds(IReadOnlyList<string> playerIds, IReadOnlyList<string> enemyIds)
        {
            if (cardDataLoader == null)
            {
                Debug.LogWarning("ICardDataLoader 미주입 — Inspector 기본 덱 사용.");
                StartBattleFromDefaults();
                return;
            }

            var playerDeck = cardDataLoader.LoadDeck(playerIds);
            var enemyDeck = cardDataLoader.LoadDeck(enemyIds);
            gameManager.InitializeBattle(
                playerDeck,
                enemyDeck,
                ResolveHero(defaultPlayerHero, "Player"),
                ResolveHero(defaultEnemyHero, "Enemy"));
        }

        public void StartBattleFromDefaults()
        {
            var player = BuildDeck(defaultPlayerDeck, "Player");
            var enemy = BuildDeck(defaultEnemyDeck, "Enemy");
            gameManager.InitializeBattle(
                player,
                enemy,
                ResolveHero(defaultPlayerHero, "Player"),
                ResolveHero(defaultEnemyHero, "Enemy"));
        }

        private HeroDataAsset ResolveHero(HeroDataAsset source, string teamPrefix)
        {
            if (source != null
                && source.normalAttackBehavior != null
                && source.shieldBehavior != null)
            {
                return source;
            }

            return RuntimeDeckFactory.CreateDefaultHero(teamPrefix);
        }

        private List<CardDataAsset> BuildDeck(CardDataAsset[] source, string teamPrefix)
        {
            var list = new List<CardDataAsset>();
            if (source != null)
            {
                for (var i = 0; i < source.Length; i++)
                {
                    if (source[i] != null)
                    {
                        list.Add(source[i]);
                    }
                }
            }

            if (list.Count == 0 || !RuntimeDeckFactory.IsDeckValid(list))
            {
                return LoadDeckFromResources(teamPrefix);
            }

            return list;
        }

        private List<CardDataAsset> LoadDeckFromResources(string teamPrefix)
        {
            var loader = cardDataLoader ?? new ResourcesCardDataLoader();
            var deck = loader.LoadDeck(DefaultDeckCatalog.GetCardIdsForTeam(teamPrefix));
            if (RuntimeDeckFactory.IsDeckValid(deck))
            {
                return deck;
            }

            Debug.LogWarning(
                $"[CardBattle] Resources 덱 로드 실패 — 런타임 기본 덱 사용 ({teamPrefix}). "
                + "CardDataAsset 일러스트가 표시되지 않을 수 있습니다.");
            return RuntimeDeckFactory.CreateDefaultDeck(teamPrefix);
        }

        public void SetResultCallback(Action<bool> callback)
        {
            gameManager.OnBattleResult = callback;
        }

        public void ReturnToMainScene(Action onReturned)
        {
            onReturned?.Invoke();
        }
    }
}
