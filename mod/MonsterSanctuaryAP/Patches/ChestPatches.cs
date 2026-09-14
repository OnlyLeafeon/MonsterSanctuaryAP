using System.Reflection;
using HarmonyLib;
using MonsterSanctuaryAP.ArchipelagoClient;
using MonsterSanctuaryAP.Mapping;
using UnityEngine.SceneManagement;

namespace MonsterSanctuaryAP
{
    [HarmonyPatch]
    public static class ChestPatches
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return typeof(Chest).GetMethod("OpenChest", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyPostfix]
        public static void Postfix(Chest __instance)
        {
            if (__instance == null) return;
            if (!ApState.IsConnected) return;

            int chestId = __instance.ID;
            string roomId = SceneManager.sceneCount > 1
                ? SceneManager.GetSceneAt(1).name
                : SceneManager.GetActiveScene().name;

            long? locationId = Locations.GetLocationId(roomId, chestId.ToString());
            if (locationId == null)
            {
                MonsterSanctuaryAPPlugin.Log.LogWarning($"[AP] No location assigned for chest {chestId} in room {roomId}");
                return;
            }

            ApState.CheckLocation(locationId.Value);
            MonsterSanctuaryAPPlugin.Log.LogInfo($"[AP] Sent check for chest {chestId} in room {roomId} (location {locationId.Value})");
        }
    }
}