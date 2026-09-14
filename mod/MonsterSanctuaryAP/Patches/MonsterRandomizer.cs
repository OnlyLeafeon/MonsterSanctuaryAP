using System;
using System.Collections.Generic;
using MonsterSanctuaryAP.ArchipelagoClient;
using UnityEngine;

namespace MonsterSanctuaryAP
{
    // Builds a single, seeded, persistent monster replacement mapping.
    // The mapping is derived from the Archipelago slot data seed, so a given
    // source monster always resolves to the same replacement across loads.
    public static class MonsterRandomizer
    {
        private static List<GameObject> _sortedPrefabs = new();
        private static List<GameObject> _shuffledPrefabs = new();
        private static Dictionary<GameObject, GameObject> _replacementMapping = new();

        public static bool IsInitialized => _replacementMapping.Count > 0;

        public static void Initialize()
        {
            if (!ApState.IsConnected) return;
            if (GameController.Instance == null) return;
            var worldData = GameController.Instance.WorldData;
            if (worldData?.Referenceables == null) return;
            if (ApState.Session?.RoomState == null) return;

            int seedHash = ApState.GetSeedHash();
            if (seedHash == 0) return;

            var prefabs = new List<GameObject>();
            foreach (var referenceable in worldData.Referenceables)
            {
                if (referenceable == null || referenceable.gameObject == null) continue;
                if (referenceable.gameObject.GetComponent<Monster>() == null) continue;
                prefabs.Add(referenceable.gameObject);
            }

            if (prefabs.Count < 2) return;

            prefabs.Sort((a, b) =>
            {
                int byName = string.CompareOrdinal(a.name, b.name);
                if (byName != 0) return byName;
                return a.GetComponent<Referenceable>().ID.CompareTo(b.GetComponent<Referenceable>().ID);
            });

            var shuffled = new List<GameObject>(prefabs);
            var rng = new System.Random(seedHash);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int k = rng.Next(i + 1);
                GameObject temp = shuffled[k];
                shuffled[k] = shuffled[i];
                shuffled[i] = temp;
            }

            var mapping = new Dictionary<GameObject, GameObject>(prefabs.Count);
            for (int i = 0; i < prefabs.Count; i++)
            {
                mapping[prefabs[i]] = shuffled[i];
            }

            _sortedPrefabs = prefabs;
            _shuffledPrefabs = shuffled;
            _replacementMapping = mapping;
        }

        public static GameObject GetReplacementMonster(GameObject original)
        {
            if (original == null) return null;
            return _replacementMapping.TryGetValue(original, out var replacement) ? replacement : null;
        }

        public static GameObject GetReplacementAt(int index)
        {
            if (index < 0 || index >= _shuffledPrefabs.Count) return null;
            return _shuffledPrefabs[index];
        }
    }
}