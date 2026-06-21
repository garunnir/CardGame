using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    internal sealed class BattleStatFloatingTextPool
    {
        private const int MaxPoolSize = 24;
        private const string PoolRootName = "BattleStatFloatPool";

        private static BattleStatFloatingTextPool shared;

        private readonly Stack<PooledEntry> available = new Stack<PooledEntry>();
        private Transform poolRoot;

        public static BattleStatFloatingTextPool Shared => shared ??= new BattleStatFloatingTextPool();

        public PooledEntry Rent()
        {
            if (available.Count > 0)
            {
                var entry = available.Pop();
                entry.Root.SetActive(true);
                return entry;
            }

            EnsurePoolRoot();
            var go = new GameObject("BattleStatFloat");
            go.transform.SetParent(poolRoot, false);
            var text = go.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            return new PooledEntry(go, text);
        }

        public void Return(PooledEntry entry)
        {
            if (entry?.Root == null)
            {
                return;
            }

            entry.Root.transform.DOKill();
            entry.Text.DOKill();
            entry.Text.text = string.Empty;
            entry.Text.color = Color.white;
            entry.Root.SetActive(false);

            if (available.Count >= MaxPoolSize)
            {
                Object.Destroy(entry.Root);
                return;
            }

            EnsurePoolRoot();
            entry.Root.transform.SetParent(poolRoot, false);
            available.Push(entry);
        }

        private void EnsurePoolRoot()
        {
            if (poolRoot != null)
            {
                return;
            }

            var existing = GameObject.Find(PoolRootName);
            poolRoot = existing != null
                ? existing.transform
                : new GameObject(PoolRootName).transform;
        }

        internal sealed class PooledEntry
        {
            public PooledEntry(GameObject root, TextMeshPro text)
            {
                Root = root;
                Text = text;
            }

            public GameObject Root { get; }
            public TextMeshPro Text { get; }
        }
    }
}
