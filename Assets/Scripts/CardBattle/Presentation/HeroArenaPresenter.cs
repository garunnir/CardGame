using System;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>3D 영웅 카드 — 스폰·동기화·연출 레지스트리.</summary>
    public sealed class HeroArenaPresenter : MonoBehaviour
    {
        [SerializeField] private BattleLayoutConfig layoutConfig;
        [SerializeField] private BattleBoardZoneLayout playerZoneLayout;
        [SerializeField] private BattleBoardZoneLayout enemyZoneLayout;
        [SerializeField] private HeroEntity playerHeroEntity;
        [SerializeField] private HeroEntity enemyHeroEntity;

        private HeroModel cachedPlayerHero;
        private HeroModel cachedEnemyHero;
        private bool enemyTargetEnabled;
        private Action enemyHeroShortClicked;

        public IHeroInputHost PlayerHeroInputHost => playerHeroEntity != null ? playerHeroEntity : null;
        public IHeroInputHost EnemyHeroInputHost => enemyHeroEntity != null ? enemyHeroEntity : null;

        public void Configure(
            BattleLayoutConfig layout,
            BattleBoardZoneLayout playerZone,
            BattleBoardZoneLayout enemyZone)
        {
            layoutConfig = layout;
            playerZoneLayout = playerZone;
            enemyZoneLayout = enemyZone;
            EnsureHeroEntities();
            ApplyLayoutToEntities();
        }

        public void BindEnemyHeroTarget(Action onEnemyHeroShortClicked)
        {
            enemyHeroShortClicked = onEnemyHeroShortClicked;
            WireEnemyShortClick();
        }

        public void SetEnemyHeroTargetEnabled(bool enabled)
        {
            enemyTargetEnabled = enabled;
            if (enemyHeroEntity != null)
            {
                enemyHeroEntity.SetTargetHighlight(enabled);
            }
        }

        public void Refresh(HeroArenaField heroArena, BattleField field)
        {
            if (heroArena == null)
            {
                return;
            }

            SanitizeEntityRefs();
            EnsureHeroEntities();
            ApplyLayoutToEntities();

            cachedPlayerHero = heroArena.PlayerHero;
            cachedEnemyHero = heroArena.EnemyHero;

            if (playerHeroEntity != null)
            {
                playerHeroEntity.SyncFromModel(cachedPlayerHero);
            }

            if (enemyHeroEntity != null)
            {
                enemyHeroEntity.SyncFromModel(cachedEnemyHero);
            }

            WireEnemyShortClick();
            SetEnemyHeroTargetEnabled(enemyTargetEnabled);
        }

        public HeroModel FindHero(HeroInstanceId id)
        {
            if (!id.IsValid)
            {
                return null;
            }

            if (cachedPlayerHero != null && cachedPlayerHero.InstanceId == id)
            {
                return cachedPlayerHero;
            }

            if (cachedEnemyHero != null && cachedEnemyHero.InstanceId == id)
            {
                return cachedEnemyHero;
            }

            return null;
        }

        public IPresentationTargetView GetPresentationView(HeroInstanceId id)
        {
            var entity = ResolveEntity(id);
            return entity != null ? new HeroPresentationTargetAdapter(entity) : null;
        }

        public IPresentationTargetView GetPresentationView(HeroModel hero)
        {
            return hero != null ? GetPresentationView(hero.InstanceId) : null;
        }

        public void SetHeroHpDisplay(HeroInstanceId id, int hp)
        {
            ResolveEntity(id)?.SetHpDisplay(hp);
        }

        public void SetHeroHpDisplay(HeroModel hero, int hp)
        {
            if (hero != null)
            {
                SetHeroHpDisplay(hero.InstanceId, hp);
            }
        }

        public async UniTask PlayStatTweenAsync(
            HeroInstanceId id,
            int hpFrom,
            int hpTo,
            int shieldFrom,
            int shieldTo,
            int mpFrom,
            int mpTo)
        {
            var entity = ResolveEntity(id);
            if (entity == null)
            {
                return;
            }

            if (hpFrom >= 0 && hpTo >= 0 && hpFrom != hpTo)
            {
                var shield = shieldFrom >= 0 ? shieldFrom : entity.DisplayShieldValue;
                await entity.TweenHpShieldAsync(
                    hpFrom,
                    hpTo,
                    shield,
                    shield,
                    entity.DisplayMaxHpValue);
            }

            if (shieldFrom >= 0 && shieldTo >= 0 && shieldFrom != shieldTo)
            {
                var hp = hpTo >= 0 ? hpTo : entity.DisplayHpValue;
                await entity.TweenHpShieldAsync(
                    hp,
                    hp,
                    shieldFrom,
                    shieldTo,
                    entity.DisplayMaxHpValue);
            }

            if (mpFrom >= 0 && mpTo >= 0 && mpFrom != mpTo)
            {
                await entity.TweenMpAsync(mpFrom, mpTo, entity.DisplayMaxMpValue);
            }
        }

        public async UniTask PlayStatTweenAsync(
            HeroModel hero,
            int hpFrom,
            int hpTo,
            int shieldFrom,
            int shieldTo,
            int mpFrom,
            int mpTo)
        {
            if (hero != null)
            {
                await PlayStatTweenAsync(
                    hero.InstanceId,
                    hpFrom,
                    hpTo,
                    shieldFrom,
                    shieldTo,
                    mpFrom,
                    mpTo);
            }
        }

        private HeroEntity ResolveEntity(HeroInstanceId id)
        {
            if (!id.IsValid)
            {
                return null;
            }

            if (playerHeroEntity != null && playerHeroEntity.InstanceId == id)
            {
                return playerHeroEntity;
            }

            if (enemyHeroEntity != null && enemyHeroEntity.InstanceId == id)
            {
                return enemyHeroEntity;
            }

            return null;
        }

        private HeroEntity ResolveEntity(HeroModel hero)
        {
            return hero != null ? ResolveEntity(hero.InstanceId) : null;
        }

        private void EnsureHeroEntities()
        {
            SanitizeEntityRefs();

            if (layoutConfig == null || layoutConfig.heroEntityPrefab == null)
            {
                return;
            }

            playerHeroEntity = EnsureHeroAtAnchor(
                playerHeroEntity,
                playerZoneLayout,
                "PlayerHeroEntity");
            enemyHeroEntity = EnsureHeroAtAnchor(
                enemyHeroEntity,
                enemyZoneLayout,
                "EnemyHeroEntity");
        }

        private static void SanitizeEntityRefs(ref HeroEntity player, ref HeroEntity enemy)
        {
            if (!player)
            {
                player = null;
            }

            if (!enemy)
            {
                enemy = null;
            }
        }

        private void SanitizeEntityRefs()
        {
            SanitizeEntityRefs(ref playerHeroEntity, ref enemyHeroEntity);
        }

        private HeroEntity EnsureHeroAtAnchor(
            HeroEntity existing,
            BattleBoardZoneLayout zoneLayout,
            string defaultName)
        {
            if (!existing)
            {
                existing = null;
            }

            var anchor = zoneLayout != null ? zoneLayout.HeroAnchor : null;
            if (anchor == null)
            {
                return null;
            }

            if (existing != null && existing.transform.parent == anchor)
            {
                return existing;
            }

            if (existing == null)
            {
                existing = anchor.GetComponentInChildren<HeroEntity>(true);
            }

            if (existing == null)
            {
                existing = Instantiate(layoutConfig.heroEntityPrefab, anchor);
                existing.name = defaultName;
            }
            else
            {
                existing.transform.SetParent(anchor, false);
            }

            existing.transform.localPosition = Vector3.zero;
            existing.transform.localRotation = Quaternion.identity;
            existing.transform.localScale = Vector3.one;
            return existing;
        }

        private void ApplyLayoutToEntities()
        {
            if (layoutConfig == null)
            {
                return;
            }

            if (playerHeroEntity != null)
            {
                playerHeroEntity.ApplyLayout(layoutConfig);
            }

            if (enemyHeroEntity != null)
            {
                enemyHeroEntity.ApplyLayout(layoutConfig);
            }
        }

        private void WireEnemyShortClick()
        {
            if (enemyHeroEntity == null)
            {
                return;
            }

            enemyHeroEntity.ShortClicked -= OnEnemyHeroShortClicked;
            enemyHeroEntity.ShortClicked += OnEnemyHeroShortClicked;
        }

        private void OnEnemyHeroShortClicked(IHeroInputHost host)
        {
            if (!enemyTargetEnabled)
            {
                return;
            }

            enemyHeroShortClicked?.Invoke();
        }

        public bool TryRaycastEnemyHero(Vector2 screenPosition, Camera camera, out HeroEntity hero)
        {
            hero = null;
            if (enemyHeroEntity == null || !enemyHeroEntity.gameObject.activeInHierarchy)
            {
                return false;
            }

            var cam = camera != null ? camera : Camera.main;
            if (cam == null)
            {
                return false;
            }

            var ray = cam.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out var hit, 200f))
            {
                return false;
            }

            if (hit.collider == null
                || !hit.collider.transform.IsChildOf(enemyHeroEntity.transform))
            {
                return false;
            }

            hero = enemyHeroEntity;
            return true;
        }

        private void OnDestroy()
        {
            if (enemyHeroEntity != null)
            {
                enemyHeroEntity.ShortClicked -= OnEnemyHeroShortClicked;
            }
        }
    }
}
