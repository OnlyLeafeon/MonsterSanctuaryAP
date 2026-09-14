using HarmonyLib;
using MonsterSanctuaryAP.ArchipelagoClient;
using UnityEngine;

namespace MonsterSanctuaryAP
{
    [HarmonyPatch(typeof(MonsterEncounter), "Start")]
    public static class MonsterEncounterStartPatch
    {
        [HarmonyPrefix]
        public static void Prefix(MonsterEncounter __instance)
        {
            if (GameController.Instance == null) return;
            if (!ApState.IsConnected) return;
            if (!MonsterSanctuaryAPPlugin._monsterrando) return;
            if (__instance.PredefinedMonsters?.Monster == null || __instance.PredefinedMonsters.Monster.Length == 0) return;
            if (__instance.GetComponent<ScriptOwner>() != null) return;

            MonsterRandomizer.Initialize();
            if (!MonsterRandomizer.IsInitialized) return;

            for (int i = 0; i < __instance.PredefinedMonsters.Monster.Length; i++)
            {
                GameObject replacement = MonsterRandomizer.GetReplacementMonster(__instance.PredefinedMonsters.Monster[i]);
                if (replacement == null) continue;
                __instance.PredefinedMonsters.Monster[i] = replacement;
            }
        }
    }
}