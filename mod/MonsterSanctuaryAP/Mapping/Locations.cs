using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace MonsterSanctuaryAP.Mapping
{
    /// <summary>
    /// Loads the embedded locations.json (Region -> Scene -> { interactableId : AP location id })
    /// and provides lookups used for sending checks and driving the map tracker.
    /// </summary>
    public static class Locations
    {
        private const string EmbeddedFileName = "MonsterSanctuaryAP.data.locations.json";

        private static readonly Dictionary<string, Dictionary<string, long>> _sceneToInteractableId = new();
        private static readonly Dictionary<string, List<long>> _regionLocationIds = new();
        private static readonly HashSet<long> _allLocationIds = new();

        public static bool IsLoaded { get; private set; }

        /// <summary>Maps "Scene_InteractableId" (e.g. "MountainPath_North3_6") to its AP location id.</summary>
        public static Dictionary<string, long> NameToId { get; private set; } = new();

        /// <summary>Reverse lookup from AP location id to "Scene_InteractableId".</summary>
        public static Dictionary<long, string> IdToName { get; private set; } = new();

        public static void Load()
        {
            NameToId = new Dictionary<string, long>();
            IdToName = new Dictionary<long, string>();
            _sceneToInteractableId.Clear();
            _regionLocationIds.Clear();
            _allLocationIds.Clear();
            IsLoaded = false;

            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(EmbeddedFileName))
                {
                    if (stream == null)
                    {
                        Debug.LogError($"[AP] Embedded resource '{EmbeddedFileName}' not found; locations unavailable.");
                        return;
                    }

                    using (var reader = new StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(json);
                        if (data == null) return;

                        foreach (var region in data)
                        {
                            var regionIds = new List<long>();

                            foreach (var scene in region.Value)
                            {
                                var interactableIds = new Dictionary<string, long>();

                                foreach (var entry in scene.Value)
                                {
                                    if (!long.TryParse(entry.Value, out long locationId)) continue;

                                    interactableIds[entry.Key] = locationId;

                                    string locationName = $"{scene.Key}_{entry.Key}";
                                    NameToId[locationName] = locationId;
                                    IdToName[locationId] = locationName;

                                    regionIds.Add(locationId);
                                    _allLocationIds.Add(locationId);
                                }

                                _sceneToInteractableId[scene.Key] = interactableIds;
                            }

                            _regionLocationIds[region.Key] = regionIds;
                        }

                        IsLoaded = true;
                        Debug.Log($"[AP] Loaded {_allLocationIds.Count} locations across {_regionLocationIds.Count} regions.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AP] Failed to load locations: {ex}");
            }
        }

        public static long? GetLocationId(string sceneName, string interactableId)
        {
            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(interactableId)) return null;
            if (!_sceneToInteractableId.TryGetValue(sceneName, out var map)) return null;
            return map.TryGetValue(interactableId, out long id) ? id : (long?)null;
        }

        public static long? GetLocationId(string locationName)
        {
            if (string.IsNullOrEmpty(locationName)) return null;
            return NameToId.TryGetValue(locationName, out long id) ? id : (long?)null;
        }

        public static string GetLocationName(long locationId)
        {
            return IdToName.TryGetValue(locationId, out string name) ? name : null;
        }

        public static bool DoesLocationExist(string locationName)
        {
            return !string.IsNullOrEmpty(locationName) && NameToId.ContainsKey(locationName);
        }

        public static bool DoesLocationExist(long locationId)
        {
            return IdToName.ContainsKey(locationId);
        }

        public static HashSet<long> GetAllLocationIds()
        {
            return new HashSet<long>(_allLocationIds);
        }

        public static IEnumerable<string> GetRegionNames()
        {
            return _regionLocationIds.Keys;
        }

        public static List<long> GetRegionLocationIds(string region)
        {
            if (region == null) return new List<long>();
            return _regionLocationIds.TryGetValue(region, out var ids) ? ids : new List<long>();
        }

        public static int GetNumberOfChecksForRegion(string region)
        {
            return region != null && _regionLocationIds.TryGetValue(region, out var ids) ? ids.Count : 0;
        }
    }
}