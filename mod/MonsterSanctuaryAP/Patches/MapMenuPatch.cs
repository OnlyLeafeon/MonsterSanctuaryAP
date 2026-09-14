using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterSanctuaryAP.ArchipelagoClient;

namespace MonsterSanctuaryAP
{
    public static class MapMenuTrackerPatches
    {
        private static void AppendAllRoomTrackerText(MapMenu menu)
        {
            List<string> lines = RoomCheckTracker.GetTrackedRooms()
                .Where(RoomCheckTracker.IsRoomTracked)
                .Select(BuildRoomLine)
                .Where(line => line != null)
                .ToList();

            if (lines.Count == 0) return;

            menu.AreaPercentText.maxChars = 500;
            lines.Insert(0, "AP Checks left:");
            menu.AreaPercentText.text = menu.AreaPercentText.text + "\n\n" + string.Join("\n", lines.ToArray());
        }

        private static void AppendCurrentRoomTrackerText(MapMenu menu)
        {
            if (GameController.Instance == null || string.IsNullOrEmpty(GameController.Instance.CurrentSceneName)) return;

            string scene = GameController.Instance.CurrentSceneName;
            string room = scene;
            if (!RoomCheckTracker.IsRoomTracked(room))
            {
                string[] parts = scene.Split('_');
                if (parts.Length > 1) room = parts[0];
            }

            if (!RoomCheckTracker.IsRoomTracked(room)) return;

            string line = BuildRoomLine(room);
            if (line == null) return;

            menu.AreaPercentText.maxChars = 500;
            menu.AreaPercentText.text = menu.AreaPercentText.text + "\n\n" + line;
        }

        private static string BuildRoomLine(string room)
        {
            int total = RoomCheckTracker.GetTotalChecks(room);
            if (total <= 0) return null;
            int completed = RoomCheckTracker.GetCompletedChecks(room);
            int remaining = RoomCheckTracker.GetRemainingChecks(room);
            return $"{room}: {completed:00} / {total:00} ({remaining} left)";
        }

        [HarmonyPatch(typeof(MapMenu), "CheckAllAreas")]
        public static class MapMenu_CheckAllAreas
        {
            [HarmonyPostfix]
            public static void Postfix(MapMenu __instance)
            {
                if (!ApState.IsConnected) return;
                AppendAllRoomTrackerText(__instance);
            }
        }

        [HarmonyPatch(typeof(MapMenu), "CheckCurrentArea")]
        public static class MapMenu_CheckCurrentArea
        {
            [HarmonyPostfix]
            public static void Postfix(MapMenu __instance)
            {
                if (!ApState.IsConnected) return;
                AppendCurrentRoomTrackerText(__instance);
            }
        }
    }
}