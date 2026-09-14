using System.Collections.Generic;
using System.Linq;
using MonsterSanctuaryAP.Mapping;

namespace MonsterSanctuaryAP.ArchipelagoClient
{
    public static class RoomCheckTracker
    {
        private static readonly Dictionary<string, List<long>> _roomToLocations = new();

        public static int GetTotalChecks(string room)
        {
            if (string.IsNullOrEmpty(room)) return 0;
            return _roomToLocations.TryGetValue(room, out var ids) ? ids.Count : 0;
        }

        public static int GetCompletedChecks(string room)
        {
            if (string.IsNullOrEmpty(room)) return 0;
            return _roomToLocations.TryGetValue(room, out var ids)
                ? ids.Count(ApState.IsLocationChecked)
                : 0;
        }

        public static int GetRemainingChecks(string room)
        {
            return GetTotalChecks(room) - GetCompletedChecks(room);
        }

        public static bool IsRoomTracked(string room)
        {
            return !string.IsNullOrEmpty(room) && _roomToLocations.ContainsKey(room);
        }

        public static IEnumerable<string> GetTrackedRooms()
        {
            return _roomToLocations.Keys;
        }

        public static void Load()
        {
            _roomToLocations.Clear();
            if (!Locations.IsLoaded) return;
            foreach (string region in Locations.GetRegionNames())
            {
                _roomToLocations[region] = Locations.GetRegionLocationIds(region).ToList();
            }
        }
    }
}