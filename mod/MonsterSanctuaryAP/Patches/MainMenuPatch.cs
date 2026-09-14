using HarmonyLib;
using UnityEngine;

namespace MonsterSanctuaryAP
{
    [HarmonyPatch(typeof(MainMenu))]
    [HarmonyPatch("Start")]
    internal class MainMenu_Start
    {
        [HarmonyPostfix]
        public static void CreateArchipelagoUI()
        {
            if (Patcher.UI != null)
                return;

            var guiObject = new GameObject("Archipelago UI");
            UnityEngine.Object.DontDestroyOnLoad(guiObject);
            Patcher.UI = guiObject.AddComponent<ArchipelagoClient.ArchipelagoUI>();
        }
    }
}
